import { connect } from 'react-redux';
import { setBookTableOption } from 'Store/Actions/bookIndexActions';
import BookIndexHeader from './BookIndexHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setBookTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(BookIndexHeader);
