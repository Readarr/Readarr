import { createAction } from 'redux-actions';
import { filterBuilderTypes, filterBuilderValueTypes, sortDirections } from 'Helpers/Props';
import sortByName from 'Utilities/Array/sortByName';
import { filterPredicates, filters, sortPredicates } from './bookActions';
import createHandleActions from './Creators/createHandleActions';
import createSetClientSideCollectionFilterReducer from './Creators/Reducers/createSetClientSideCollectionFilterReducer';
import createSetClientSideCollectionSortReducer from './Creators/Reducers/createSetClientSideCollectionSortReducer';
import createSetTableOptionReducer from './Creators/Reducers/createSetTableOptionReducer';

//
// Variables

export const section = 'bookIndex';

//
// State

export const defaultState = {
  sortKey: 'title',
  sortDirection: sortDirections.ASCENDING,
  secondarySortKey: 'title',
  secondarySortDirection: sortDirections.ASCENDING,
  view: 'posters',

  posterOptions: {
    detailedProgressBar: false,
    size: 'large',
    showTitle: true,
    showAuthor: true,
    showMonitored: true,
    showQualityProfile: true,
    showSearchAction: false
  },

  overviewOptions: {
    detailedProgressBar: false,
    size: 'medium',
    showReleaseDate: true,
    showMonitored: true,
    showQualityProfile: true,
    showAdded: false,
    showPath: false,
    showSizeOnDisk: false,
    showSearchAction: false
  },

  tableOptions: {
    showSearchAction: false
  },

  columns: [
    {
      name: 'status',
      columnLabel: 'Status',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'title',
      label: 'Book',
      isSortable: true,
      isVisible: true,
      isModifiable: false
    },
    {
      name: 'authorName',
      label: 'Author',
      isSortable: true,
      isVisible: true,
      isModifiable: true
    },
    {
      name: 'releaseDate',
      label: 'Release Date',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'qualityProfileId',
      label: 'Quality Profile',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'added',
      label: 'Added',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'bookFileCount',
      label: 'File Count',
      isSortable: true,
      isVisible: true
    },
    {
      name: 'path',
      label: 'Path',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'sizeOnDisk',
      label: 'Size on Disk',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'genres',
      label: 'Genres',
      isSortable: false,
      isVisible: false
    },
    {
      name: 'ratings',
      label: 'Rating',
      isSortable: true,
      isVisible: false
    },
    {
      name: 'tags',
      label: 'Tags',
      isSortable: false,
      isVisible: false
    },
    {
      name: 'actions',
      columnLabel: 'Actions',
      isVisible: true,
      isModifiable: false
    }
  ],

  sortPredicates: {
    ...sortPredicates,

    bookFileCount: function(item) {
      const { statistics = {} } = item;

      return statistics.bookCount || 0;
    },

    ratings: function(item) {
      const { ratings = {} } = item;

      return ratings.value;
    }
  },

  selectedFilterKey: 'all',

  filters,

  filterPredicates: {
    ...filterPredicates
  },

  filterBuilderProps: [
    {
      name: 'monitored',
      label: 'Monitored',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.BOOL
    },
    {
      name: 'qualityProfileId',
      label: 'Quality Profile',
      type: filterBuilderTypes.EXACT,
      valueType: filterBuilderValueTypes.QUALITY_PROFILE
    },
    {
      name: 'releaseDate',
      label: 'Release Date',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'added',
      label: 'Added',
      type: filterBuilderTypes.DATE,
      valueType: filterBuilderValueTypes.DATE
    },
    {
      name: 'bookFileCount',
      label: 'File Count',
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'path',
      label: 'Path',
      type: filterBuilderTypes.STRING
    },
    {
      name: 'sizeOnDisk',
      label: 'Size on Disk',
      type: filterBuilderTypes.NUMBER,
      valueType: filterBuilderValueTypes.BYTES
    },
    {
      name: 'genres',
      label: 'Genres',
      type: filterBuilderTypes.ARRAY,
      optionsSelector: function(items) {
        const tagList = items.reduce((acc, Book) => {
          Book.genres.forEach((genre) => {
            acc.push({
              id: genre,
              name: genre
            });
          });

          return acc;
        }, []);

        return tagList.sort(sortByName);
      }
    },
    {
      name: 'ratings',
      label: 'Rating',
      type: filterBuilderTypes.NUMBER
    },
    {
      name: 'tags',
      label: 'Tags',
      type: filterBuilderTypes.ARRAY,
      valueType: filterBuilderValueTypes.TAG
    }
  ]
};

export const persistState = [
  'bookIndex.sortKey',
  'bookIndex.sortDirection',
  'bookIndex.selectedFilterKey',
  'bookIndex.customFilters',
  'bookIndex.view',
  'bookIndex.columns',
  'bookIndex.posterOptions',
  'bookIndex.bannerOptions',
  'bookIndex.overviewOptions',
  'bookIndex.tableOptions'
];

//
// Actions Types

export const SET_BOOK_SORT = 'bookIndex/setBookSort';
export const SET_BOOK_FILTER = 'bookIndex/setBookFilter';
export const SET_BOOK_VIEW = 'bookIndex/setBookView';
export const SET_BOOK_TABLE_OPTION = 'bookIndex/setBookTableOption';
export const SET_BOOK_POSTER_OPTION = 'bookIndex/setBookPosterOption';
export const SET_BOOK_BANNER_OPTION = 'bookIndex/setBookBannerOption';
export const SET_BOOK_OVERVIEW_OPTION = 'bookIndex/setBookOverviewOption';

//
// Action Creators

export const setBookSort = createAction(SET_BOOK_SORT);
export const setBookFilter = createAction(SET_BOOK_FILTER);
export const setBookView = createAction(SET_BOOK_VIEW);
export const setBookTableOption = createAction(SET_BOOK_TABLE_OPTION);
export const setBookPosterOption = createAction(SET_BOOK_POSTER_OPTION);
export const setBookBannerOption = createAction(SET_BOOK_BANNER_OPTION);
export const setBookOverviewOption = createAction(SET_BOOK_OVERVIEW_OPTION);

//
// Reducers

export const reducers = createHandleActions({

  [SET_BOOK_SORT]: createSetClientSideCollectionSortReducer(section),
  [SET_BOOK_FILTER]: createSetClientSideCollectionFilterReducer(section),

  [SET_BOOK_VIEW]: function(state, { payload }) {
    return Object.assign({}, state, { view: payload.view });
  },

  [SET_BOOK_TABLE_OPTION]: createSetTableOptionReducer(section),

  [SET_BOOK_POSTER_OPTION]: function(state, { payload }) {
    const posterOptions = state.posterOptions;

    return {
      ...state,
      posterOptions: {
        ...posterOptions,
        ...payload
      }
    };
  },

  [SET_BOOK_BANNER_OPTION]: function(state, { payload }) {
    const bannerOptions = state.bannerOptions;

    return {
      ...state,
      bannerOptions: {
        ...bannerOptions,
        ...payload
      }
    };
  },

  [SET_BOOK_OVERVIEW_OPTION]: function(state, { payload }) {
    const overviewOptions = state.overviewOptions;

    return {
      ...state,
      overviewOptions: {
        ...overviewOptions,
        ...payload
      }
    };
  }

}, defaultState, section);
