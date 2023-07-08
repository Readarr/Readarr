using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Http.REST.Attributes;
using Readarr.Http;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Profiles.Metadata
{
    [V1ApiController]
    public class MetadataProfileController : RestController<MetadataProfileResource>
    {
        private readonly IMetadataProfileService _profileService;

        public MetadataProfileController(IMetadataProfileService profileService)
        {
            _profileService = profileService;

            SharedValidator.RuleFor(c => c.Name)
                .NotEqual("None").WithMessage("'None' is a reserved profile name")
                .NotEmpty();
            SharedValidator.RuleFor(c => c.MinPopularity).GreaterThanOrEqualTo(0);
            SharedValidator.RuleFor(c => c.MinPages).GreaterThanOrEqualTo(0);
            SharedValidator.RuleFor(c => c.AllowedLanguages)
                .Must(x => x
                    .Trim(',')
                    .Split(',')
                    .Select(y => y.Trim())
                    .All(y => y == "null" || NzbDrone.Core.Books.Calibre.Extensions.KnownLanguages.Contains(y)))
                .When(x => x.AllowedLanguages.IsNotNullOrWhiteSpace())
                .WithMessage("Unknown languages");
        }

        [RestPostById]
        public ActionResult<MetadataProfileResource> Create(MetadataProfileResource resource)
        {
            var model = resource.ToModel();
            model = _profileService.Add(model);
            return Created(model.Id);
        }

        [RestDeleteById]
        public void DeleteProfile(int id)
        {
            _profileService.Delete(id);
        }

        [RestPutById]
        public ActionResult<MetadataProfileResource> Update(MetadataProfileResource resource)
        {
            var model = resource.ToModel();

            _profileService.Update(model);

            return Accepted(model.Id);
        }

        protected override MetadataProfileResource GetResourceById(int id)
        {
            return _profileService.Get(id).ToResource();
        }

        [HttpGet]
        public List<MetadataProfileResource> GetAll()
        {
            var profiles = _profileService.All().ToResource();

            return profiles;
        }
    }
}
