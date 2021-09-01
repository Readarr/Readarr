import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setBookPosterOption } from 'Store/Actions/bookIndexActions';
import BookIndexPosterOptionsModalContent from './BookIndexPosterOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.bookIndex,
    (bookIndex) => {
      return bookIndex.posterOptions;
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangePosterOption(payload) {
      dispatch(setBookPosterOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(BookIndexPosterOptionsModalContent);
