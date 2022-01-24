namespace NzbDrone.Core.CustomFormats
{
    public class ReleaseTitleSpecification : RegexSpecificationBase
    {
        public override int Order => 1;
        public override string ImplementationName => "Release Title";
        public override string InfoLink => "https://wiki.servarr.com/readarr/settings#custom-formats-2";

        protected override bool IsSatisfiedByWithoutNegate(CustomFormatInput input)
        {
            return MatchString(input.BookInfo?.ReleaseTitle) || MatchString(input.Filename);
        }
    }
}
