using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.Books
{
    public interface IEditionService
    {
        Edition GetEdition(int id);
        Edition GetEditionByForeignEditionId(string foreignEditionId);
        List<Edition> GetAllMonitoredEditions();
        void InsertMany(List<Edition> editions);
        void UpdateMany(List<Edition> editions);
        void DeleteMany(List<Edition> editions);
        List<Edition> GetEditionsForRefresh(int bookId, IEnumerable<string> foreignEditionIds);
        List<Edition> GetEditionsByBook(int bookId);
        List<Edition> GetEditionsByAuthor(int authorId);
        Edition FindByTitle(int authorMetadataId, string title);
        Edition FindByTitleInexact(int authorMetadataId, string title);
        List<Edition> GetCandidates(int authorMetadataId, string title);
        List<Edition> SetMonitored(Edition edition);
    }

    public class EditionService : IEditionService,
        IHandle<BookDeletedEvent>
    {
        private readonly IEditionRepository _editionRepository;
        private readonly IEventAggregator _eventAggregator;

        public EditionService(IEditionRepository editionRepository,
                              IEventAggregator eventAggregator)
        {
            _editionRepository = editionRepository;
            _eventAggregator = eventAggregator;
        }

        public Edition GetEdition(int id)
        {
            return _editionRepository.Get(id);
        }

        public Edition GetEditionByForeignEditionId(string foreignEditionId)
        {
            return _editionRepository.FindByForeignEditionId(foreignEditionId);
        }

        public List<Edition> GetAllMonitoredEditions()
        {
            return _editionRepository.GetAllMonitoredEditions();
        }

        public void InsertMany(List<Edition> editions)
        {
            _editionRepository.InsertMany(editions);
        }

        public void UpdateMany(List<Edition> editions)
        {
            _editionRepository.UpdateMany(editions);
        }

        public void DeleteMany(List<Edition> editions)
        {
            _editionRepository.DeleteMany(editions);
            foreach (var edition in editions)
            {
                _eventAggregator.PublishEvent(new EditionDeletedEvent(edition));
            }
        }

        public List<Edition> GetEditionsForRefresh(int bookId, IEnumerable<string> foreignEditionIds)
        {
            return _editionRepository.GetEditionsForRefresh(bookId, foreignEditionIds);
        }

        public List<Edition> GetEditionsByBook(int bookId)
        {
            return _editionRepository.FindByBook(bookId);
        }

        public List<Edition> GetEditionsByAuthor(int authorId)
        {
            return _editionRepository.FindByAuthor(authorId);
        }

        public Edition FindByTitle(int authorMetadataId, string title)
        {
            return _editionRepository.FindByTitle(authorMetadataId, title);
        }

        public Edition FindByTitleInexact(int authorMetadataId, string title)
        {
            var books = _editionRepository.FindByAuthorMetadataId(authorMetadataId, true);

            foreach (var func in EditionScoringFunctions(title))
            {
                var results = FindByStringInexact(books, func.Item1, func.Item2);
                if (results.Count == 1)
                {
                    return results[0];
                }
            }

            return null;
        }

        public List<Edition> GetCandidates(int authorMetadataId, string title)
        {
            var books = _editionRepository.FindByAuthorMetadataId(authorMetadataId, true);
            var output = new List<Edition>();

            foreach (var func in EditionScoringFunctions(title))
            {
                output.AddRange(FindByStringInexact(books, func.Item1, func.Item2));
            }

            return output.DistinctBy(x => x.Id).ToList();
        }

        public List<Edition> SetMonitored(Edition edition)
        {
            return _editionRepository.SetMonitored(edition);
        }

        public void Handle(BookDeletedEvent message)
        {
            var editions = GetEditionsByBook(message.Book.Id);
            DeleteMany(editions);
        }

        private List<Tuple<Func<Edition, string, double>, string>> EditionScoringFunctions(string title)
        {
            Func<Func<Edition, string, double>, string, Tuple<Func<Edition, string, double>, string>> tc = Tuple.Create;
            var scoringFunctions = new List<Tuple<Func<Edition, string, double>, string>>
            {
                tc((a, t) => a.Title.FuzzyMatch(t), title),
                tc((a, t) => a.Title.FuzzyMatch(t), title.RemoveBracketsAndContents().CleanAuthorName()),
                tc((a, t) => a.Title.FuzzyMatch(t), title.RemoveAfterDash().CleanAuthorName()),
                tc((a, t) => a.Title.FuzzyMatch(t), title.RemoveBracketsAndContents().RemoveAfterDash().CleanAuthorName()),
                tc((a, t) => t.FuzzyContains(a.Title), title)
            };

            return scoringFunctions;
        }

        private List<Edition> FindByStringInexact(List<Edition> editions, Func<Edition, string, double> scoreFunction, string title)
        {
            const double fuzzThreshold = 0.7;
            const double fuzzGap = 0.4;

            var sortedEditions = editions.Select(s => new
                {
                    MatchProb = scoreFunction(s, title),
                    Edition = s
                })
                .ToList()
                .OrderByDescending(s => s.MatchProb)
                .ToList();

            return sortedEditions.TakeWhile((x, i) => i == 0 || sortedEditions[i - 1].MatchProb - x.MatchProb < fuzzGap)
                .TakeWhile((x, i) => x.MatchProb > fuzzThreshold || (i > 0 && sortedEditions[i - 1].MatchProb > fuzzThreshold))
                .Select(x => x.Edition)
                .ToList();
        }
    }
}
