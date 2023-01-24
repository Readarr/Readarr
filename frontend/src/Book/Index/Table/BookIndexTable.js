import PropTypes from 'prop-types';
import React, { Component } from 'react';
import BookIndexItemConnector from 'Book/Index/BookIndexItemConnector';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import { sortDirections } from 'Helpers/Props';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import BookIndexHeaderConnector from './BookIndexHeaderConnector';
import BookIndexRow from './BookIndexRow';
import styles from './BookIndexTable.css';

class BookIndexTable extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      scrollIndex: null
    };
  }

  componentDidUpdate(prevProps) {
    const {
      items,
      sortKey,
      jumpToCharacter
    } = this.props;

    if (jumpToCharacter != null && jumpToCharacter !== prevProps.jumpToCharacter) {

      const scrollIndex = getIndexOfFirstCharacter(items, sortKey, jumpToCharacter);

      if (scrollIndex != null) {
        this.setState({ scrollIndex });
      }
    } else if (jumpToCharacter == null && prevProps.jumpToCharacter != null) {
      this.setState({ scrollIndex: null });
    }
  }

  //
  // Control

  rowRenderer = ({ key, rowIndex, style }) => {
    const {
      items,
      columns,
      selectedState,
      onSelectedChange,
      isEditorActive
    } = this.props;

    const book = items[rowIndex];

    return (
      <VirtualTableRow
        key={key}
        style={style}
      >
        <BookIndexItemConnector
          key={book.id}
          component={BookIndexRow}
          style={style}
          columns={columns}
          authorId={book.authorId}
          bookId={book.id}
          isSelected={selectedState[book.id]}
          onSelectedChange={onSelectedChange}
          isEditorActive={isEditorActive}
        />
      </VirtualTableRow>
    );
  };

  //
  // Render

  render() {
    const {
      items,
      columns,
      sortKey,
      sortDirection,
      isSmallScreen,
      onSortPress,
      scroller,
      scrollTop,
      allSelected,
      allUnselected,
      onSelectAllChange,
      isEditorActive,
      selectedState
    } = this.props;

    return (
      <VirtualTable
        className={styles.tableContainer}
        items={items}
        scrollIndex={this.state.scrollIndex}
        scrollTop={scrollTop}
        isSmallScreen={isSmallScreen}
        scroller={scroller}
        rowHeight={38}
        overscanRowCount={2}
        rowRenderer={this.rowRenderer}
        header={
          <BookIndexHeaderConnector
            columns={columns}
            sortKey={sortKey}
            sortDirection={sortDirection}
            onSortPress={onSortPress}
            allSelected={allSelected}
            allUnselected={allUnselected}
            onSelectAllChange={onSelectAllChange}
            isEditorActive={isEditorActive}
          />
        }
        selectedState={selectedState}
        columns={columns}
        sortKey={sortKey}
        sortDirection={sortDirection}
      />
    );
  }
}

BookIndexTable.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  jumpToCharacter: PropTypes.string,
  scrollTop: PropTypes.number,
  scroller: PropTypes.instanceOf(Element).isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  onSortPress: PropTypes.func.isRequired,
  allSelected: PropTypes.bool.isRequired,
  allUnselected: PropTypes.bool.isRequired,
  selectedState: PropTypes.object.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  onSelectAllChange: PropTypes.func.isRequired,
  isEditorActive: PropTypes.bool.isRequired
};

export default BookIndexTable;
