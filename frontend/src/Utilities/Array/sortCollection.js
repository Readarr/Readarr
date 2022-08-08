import _ from 'lodash';
import { sortDirections } from 'Helpers/Props';

function getSortClause(sortKey, sortDirection, sortPredicates) {
  if (sortPredicates && sortPredicates.hasOwnProperty(sortKey)) {
    return function(item) {
      return sortPredicates[sortKey](item, sortDirection);
    };
  }

  return function(item) {
    return item[sortKey];
  };
}

function sortCollection(items, state) {
  const {
    sortKey,
    sortDirection,
    sortPredicates,
    secondarySortKey,
    secondarySortDirection
  } = state;

  const clauses = [];
  const orders = [];

  clauses.push(getSortClause(sortKey, sortDirection, sortPredicates));
  orders.push(sortDirection === sortDirections.ASCENDING ? 'asc' : 'desc');

  if (secondarySortKey &&
        secondarySortDirection &&
        (sortKey !== secondarySortKey ||
         sortDirection !== secondarySortDirection)) {
    clauses.push(getSortClause(secondarySortKey, secondarySortDirection, sortPredicates));
    orders.push(secondarySortDirection === sortDirections.ASCENDING ? 'asc' : 'desc');
  }

  return _.orderBy(items, clauses, orders);
}

export default sortCollection;
