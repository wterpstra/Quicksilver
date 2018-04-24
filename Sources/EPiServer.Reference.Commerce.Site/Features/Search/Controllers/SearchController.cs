﻿using EPiServer.Tracking.Commerce.Data;
using EPiServer.Reference.Commerce.Site.Features.Recommendations.Extensions;
using EPiServer.Reference.Commerce.Site.Features.Recommendations.Services;
using EPiServer.Reference.Commerce.Site.Features.Search.Pages;
using EPiServer.Reference.Commerce.Site.Features.Search.Services;
using EPiServer.Reference.Commerce.Site.Features.Search.ViewModelFactories;
using EPiServer.Reference.Commerce.Site.Features.Search.ViewModels;
using EPiServer.Web.Mvc;
using Mediachase.Commerce.Catalog;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace EPiServer.Reference.Commerce.Site.Features.Search.Controllers
{
    public class SearchController : PageController<SearchPage>
    {
        private readonly SearchViewModelFactory _viewModelFactory;
        private readonly ISearchService _searchService;
        private readonly IRecommendationService _recommendationService;
        private readonly ReferenceConverter _referenceConverter;

        public SearchController(
            SearchViewModelFactory viewModelFactory, 
            ISearchService searchService,
            IRecommendationService recommendationService,
            ReferenceConverter referenceConverter)
        {
            _viewModelFactory = viewModelFactory;
            _searchService = searchService;
            _recommendationService = recommendationService;
            _referenceConverter = referenceConverter;
        }

        [ValidateInput(false)]
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public async Task<ActionResult> Index(SearchPage currentPage, FilterOptionViewModel filterOptions)
        {
            var viewModel = _viewModelFactory.Create(currentPage, filterOptions);

            if (filterOptions.Page <= 1 && HttpContext.Request.HttpMethod == "GET")
            {
                HttpContext.Items[SearchTrackingData.TotalSearchResultsKey] = filterOptions.TotalCount;

                var trackingResult =
                    await _recommendationService.TrackSearchAsync(HttpContext, filterOptions.Q,
                        viewModel.ProductViewModels.Select(x => x.Code));
                viewModel.Recommendations = trackingResult.GetSearchResultRecommendations(_referenceConverter);
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult QuickSearch(string q = "")
        {
            var result = _searchService.QuickSearch(q);
            return View("_QuickSearch", result);
        }
    }
}