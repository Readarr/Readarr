using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.NYTimes
{
    public class NYTimesResponse<T>
    {
        public string Status { get; set; }
        public string Copyright { get; set; }
        [JsonProperty(PropertyName = "num_results")]
        public int NumResults { get; set; }
        [JsonProperty(PropertyName = "last_modified")]
        public string LastModified { get; set; }
        public T Results { get; set; }
    }

    public class NYTimesName
    {
        [JsonProperty(PropertyName = "list_name")]
        public string ListName { get; set; }
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }
        [JsonProperty(PropertyName = "list_name_encoded")]
        public string ListNameEncoded { get; set; }
        [JsonProperty(PropertyName = "oldest_published_date")]
        public string OldestPublishedDate { get; set; }
        [JsonProperty(PropertyName = "newest_published_date")]
        public string NewestPublishedDate { get; set; }
        public string Updated { get; set; }
    }

    public class NYTimesList
    {
        [JsonProperty(PropertyName = "list_name")]
        public string ListName { get; set; }
        [JsonProperty(PropertyName = "display_name")]
        public string DisplayName { get; set; }
        [JsonProperty(PropertyName = "bestsellers_date")]
        public string BestsellersDate { get; set; }
        [JsonProperty(PropertyName = "published_date")]
        public string PublishedDate { get; set; }
        public int Rank { get; set; }
        [JsonProperty(PropertyName = "rank_last_week")]
        public int RankLastWeek { get; set; }
        [JsonProperty(PropertyName = "weeks_on_list")]
        public int WeeksonList { get; set; }
        public int Asterisk { get; set; }
        public int Dagger { get; set; }
        [JsonProperty(PropertyName = "amazon_product_url")]
        public string AmazonProductUrl { get; set; }
        public object[] Isbns { get; set; }
        [JsonProperty(PropertyName = "book_details")]
        public List<NYTimesBook> BookDetails { get; set; }
        public object[] Reviews { get; set; }
    }

    public class NYTimesBook
    {
        [JsonProperty(PropertyName = "age_group")]
        public string AgeGroup { get; set; }
        public string Author { get; set; }
        public string Contributor { get; set; }
        [JsonProperty(PropertyName = "contributor_note")]
        public string ContributorNote { get; set; }
        [JsonProperty(PropertyName = "created_date")]
        public string CreatedDate { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        [JsonProperty(PropertyName = "primary_isbn13")]
        public string PrimaryIsbn13 { get; set; }
        [JsonProperty(PropertyName = "primary_isbn10")]
        public string PrimaryIsbn10 { get; set; }
        public string Publisher { get; set; }
        public int Rank { get; set; }
        public string Title { get; set; }
        [JsonProperty(PropertyName = "updated_date")]
        public string UpdatedDate { get; set; }
    }
}
