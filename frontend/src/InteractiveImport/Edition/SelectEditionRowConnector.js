import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import SelectEditionRow from './SelectEditionRow';

function createMapStateToProps() {
  return createSelector(
    (state, { id }) => id,
    (state) => state.editions,
    (id, editionState) => {
      const editions = editionState.items.filter((e) => e.bookId === id);
      return { editions };
    }
  );
}

class SelectEditionRowConnector extends Component {
  render() {
    return (
      <SelectEditionRow
        {...this.props}
      />
    );
  }
}

SelectEditionRowConnector.PropTypes = {
  editions: PropTypes.arrayOf(PropTypes.object).isRequired
};

export default connect(createMapStateToProps)(SelectEditionRowConnector);
