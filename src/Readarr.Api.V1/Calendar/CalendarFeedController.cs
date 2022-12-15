using System;
using System.Collections.Generic;
using System.Linq;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Tags;
using Readarr.Http;

namespace Readarr.Api.V1.Calendar
{
    [V1FeedController("calendar")]
    public class CalendarFeedController : Controller
    {
        private readonly IBookService _bookService;
        private readonly IAuthorService _authorService;
        private readonly ITagService _tagService;

        public CalendarFeedController(IBookService bookService, IAuthorService authorService, ITagService tagService)
        {
            _bookService = bookService;
            _authorService = authorService;
            _tagService = tagService;
        }

        [HttpGet("Readarr.ics")]
        public IActionResult GetCalendarFeed(int pastDays = 7, int futureDays = 28, string tagList = "", bool unmonitored = false)
        {
            var start = DateTime.Today.AddDays(-pastDays);
            var end = DateTime.Today.AddDays(futureDays);
            var tags = new List<int>();

            if (tagList.IsNotNullOrWhiteSpace())
            {
                tags.AddRange(tagList.Split(',').Select(_tagService.GetTag).Select(t => t.Id));
            }

            var books = _bookService.BooksBetweenDates(start, end, unmonitored);
            var calendar = new Ical.Net.Calendar
            {
                ProductId = "-//readarr.com//Readarr//EN"
            };

            var calendarName = "Readarr Book Schedule";
            calendar.AddProperty(new CalendarProperty("NAME", calendarName));
            calendar.AddProperty(new CalendarProperty("X-WR-CALNAME", calendarName));

            foreach (var book in books.OrderBy(v => v.ReleaseDate.Value))
            {
                var author = _authorService.GetAuthor(book.AuthorId); // Temp fix TODO: Figure out why Book.Author is not populated during BooksBetweenDates Query

                if (tags.Any() && tags.None(author.Tags.Contains))
                {
                    continue;
                }

                var occurrence = calendar.Create<CalendarEvent>();
                occurrence.Uid = "Readarr_book_" + book.Id;

                //occurrence.Status = book.HasFile ? EventStatus.Confirmed : EventStatus.Tentative;
                occurrence.Description = book.Editions.Value.Single(x => x.Monitored).Overview;
                occurrence.Categories = book.Genres;

                occurrence.Start = new CalDateTime(book.ReleaseDate.Value.ToLocalTime()) { HasTime = false };
                occurrence.End = occurrence.Start;
                occurrence.IsAllDay = true;

                occurrence.Summary = $"{author.Name} - {book.Title}";
            }

            var serializer = (IStringSerializer)new SerializerFactory().Build(calendar.GetType(), new SerializationContext());
            var icalendar = serializer.SerializeToString(calendar);

            return Content(icalendar, "text/calendar");
        }
    }
}
