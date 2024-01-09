using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using Readarr.Http.REST;

namespace Readarr.Api.V1.OPDS
{
    public class OPDSCatalogMetadataResource : IEmbeddedDocument
    {
        public string Title { get; set; }
    }

    public class OPDSLinkResource : IEmbeddedDocument
    {
        public string Title { get; set; }
        public string Rel { get; set; }
        public string Href { get; set; }
        public string Type { get; set; }
    }

    public class OPDSPublicationMetadataResource : IEmbeddedDocument
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string @Type { get; set; }
        public string Author { get; set; }
        public string Identifier { get; set; }
        public string Language { get; set; }
        public DateTime Modified { get; set; }
        public string Description { get; set; }
        public List<string> Genres { get; set; }
        public double Rating { get; set; }
        public int Votes { get; set; }
        public string ForeignBookId { get; set; }
    }

    public class OPDSImageResource : IEmbeddedDocument
    {
        public string Href { get; set; }
        public string Type { get; set; }
    }

    public class OPDSPublicationResource : IEmbeddedDocument
    {
        public OPDSPublicationMetadataResource Metadata { get; set; }
        public List<OPDSLinkResource> Links { get; set; }
        public List<OPDSImageResource> Images { get; set; }
    }

    public class OPDSCatalogResource : RestResource
    {
        public OPDSCatalogMetadataResource Metadata { get; set; }
        public List<OPDSLinkResource> Links { get; set; }
        public List<OPDSLinkResource> Navigation { get; set; }
        public List<OPDSPublicationResource> Publications { get; set; }
    }

    public class OPDSPublicationsResource : RestResource
    {
        public OPDSCatalogMetadataResource Metadata { get; set; }
        public List<OPDSLinkResource> Links { get; set; }
        public List<OPDSPublicationResource> Publications { get; set; }
    }

    public static class OPDSResourceMapper
    {
        public static OPDSCatalogResource ToOPDSCatalogResource()
        {
            var self = new OPDSLinkResource
            {
                Href = "/opds",
                Rel = "self",
                Title = "Readarr OPDS Catalog",
                Type = "application/opds+json"
            };

            var links = new List<OPDSLinkResource>();
            links.Add(self);

            var nav = new List<OPDSLinkResource>();
            var pubs = new OPDSLinkResource
            {
                Href = "/opds/publications",
                Rel = "self",
                Title = "Readarr OPDS Available Publications",
                Type = "application/opds+json"
            };
            nav.Add(pubs);

            var meta = new OPDSCatalogMetadataResource
            {
                Title = self.Title
            };

            var wanted = new OPDSLinkResource
            {
                Href = "/opds/wanted",
                Rel = "self",
                Title = "Readarr OPDS Wanted Publications",
                Type = "application/opds+json"
            };
            nav.Add(wanted);

            return new OPDSCatalogResource
            {
                Metadata = meta,
                Links = links,
                Navigation = nav,
                Publications = new List<OPDSPublicationResource>()
            };
        }

        public static OPDSPublicationsResource ToOPDSPublicationsResource()
        {
            var self = new OPDSLinkResource
            {
                Href = "/opds/publications",
                Rel = "self",
                Title = "Readarr OPDS Publications",
                Type = "application/opds+json"
            };

            var links = new List<OPDSLinkResource>();
            links.Add(self);

            var meta = new OPDSCatalogMetadataResource
            {
                Title = self.Title
            };

            return new OPDSPublicationsResource
            {
                Metadata = meta,
                Links = links,
                Publications = new List<OPDSPublicationResource>()
            };
        }

        public static OPDSPublicationMetadataResource ToOPDSPublicationMetadataResource(Book book)
        {
            var edition = book.Editions?.Value.Where(x => x.Monitored).SingleOrDefault();
            return new OPDSPublicationMetadataResource
            {
                Id = book.Id,
                Title = book.Title,
                @Type = "http://schema.org/Book",
                Author = book.Author.Value?.Metadata?.Value?.SortNameLastFirst ?? book.Author.Value.Name,
                Identifier = edition.Isbn13,
                Language = edition.Language,
                Modified = book.ReleaseDate ?? DateTime.Now,
                Description = edition.Overview,
                Genres = book.Genres,
                Votes = book.Ratings.Votes,
                Rating = (double)book.Ratings.Value,
                ForeignBookId = book.ForeignBookId,
            };
        }

        public static OPDSImageResource ToOPDSImageResource(MediaCover image)
        {
            return new OPDSImageResource
            {
                Href = image.Url,
                Type = GetImageContentType(image.Extension)
            };
        }

        public static OPDSPublicationResource ToOPDSPublicationResource(Book book, List<BookFile> files, Edition edition, List<MediaCover> covers)
        {
            var linkResources = new List<OPDSLinkResource>();
            var imageResources = new List<OPDSImageResource>();

            //Must have link to self
            linkResources.Add(new OPDSLinkResource
            {
                Href = string.Format("opds/publications/{0}", book.Id),
                Rel = "self",
                Title = book.Title,
                Type = "application/opds-publication+json"
            });

            if (files.Count > 0)
            {
                //we'll only add the first bookfile (for now)
                foreach (var file in files)
                {
                    linkResources.Add(new OPDSLinkResource
                    {
                        Href = string.Format("bookfile/download/{0}", book.Id),
                        Rel = "http://opds-spec.org/acquisition",
                        Title = string.Format("Readarr OPDS Link:{0}", book.Id),
                        Type = GetContentType(file.Path)
                    });
                    break;
                }
            }
            else if (edition != null)
            {
                linkResources.Add(new OPDSLinkResource
                {
                    Href = edition.Links.First().Url,
                    Rel = "http://opds-spec.org/acquisition",
                    Title = string.Format("Readarr OPDS Link:{0}", book.Id),
                    Type = GetContentType("test.epub")
                });
            }

            foreach (var cover in covers)
            {
                var imageResource = ToOPDSImageResource(cover);
                imageResources.Add(imageResource);
            }

            return new OPDSPublicationResource
            {
                Metadata = ToOPDSPublicationMetadataResource(book),
                Links = linkResources,
                Images = imageResources
            };
        }

        private static string GetContentType(string filePath)
        {
            var contentType = "application/octet-stream";
            var ext = PathExtensions.GetPathExtension(filePath);
            if (ext.Contains("epub"))
            {
                contentType = "application/epub+zip";
            }
            else if (ext.Contains("azw"))
            {
                contentType = "application/vnd.amazon.ebook";
            }
            else if (ext.Contains("azw"))
            {
                contentType = "application/x-mobipocket-ebook";
            }
            else if (ext.Contains("pdf"))
            {
                contentType = "application/pdf";
            }

            return contentType;
        }

        private static string GetImageContentType(string ext)
        {
            var contentType = string.Format("application/{0}", ext);
            if (ext.Contains("jpg") || ext.Contains("jpeg"))
            {
                contentType = "image/jpeg";
            }
            else if (ext.Contains("png"))
            {
                contentType = "image/png";
            }
            else if (ext.Contains("gif"))
            {
                contentType = "image/gif";
            }
            else if (ext.Contains("svg"))
            {
                contentType = "image/svg+xml";
            }

            return contentType;
        }
    }
}
