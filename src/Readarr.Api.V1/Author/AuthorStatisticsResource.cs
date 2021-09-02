using NzbDrone.Core.AuthorStats;

namespace Readarr.Api.V1.Author
{
    public class AuthorStatisticsResource
    {
        public int BookFileCount { get; set; }
        public int BookCount { get; set; }
        public int AvailableBookCount { get; set; }
        public int TotalBookCount { get; set; }
        public long SizeOnDisk { get; set; }

        public decimal PercentOfBooks
        {
            get
            {
                if (BookCount == 0)
                {
                    return 0;
                }

                return AvailableBookCount / (decimal)BookCount * 100;
            }
        }
    }

    public static class AuthorStatisticsResourceMapper
    {
        public static AuthorStatisticsResource ToResource(this AuthorStatistics model)
        {
            if (model == null)
            {
                return null;
            }

            return new AuthorStatisticsResource
            {
                BookFileCount = model.BookFileCount,
                BookCount = model.BookCount,
                AvailableBookCount = model.AvailableBookCount,
                TotalBookCount = model.TotalBookCount,
                SizeOnDisk = model.SizeOnDisk
            };
        }
    }
}
