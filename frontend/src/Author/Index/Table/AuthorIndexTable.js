import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AuthorIndexItemConnector from 'Author/Index/AuthorIndexItemConnector';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import { sortDirections } from 'Helpers/Props';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import AuthorIndexHeaderConnector from './AuthorIndexHeaderConnector';
import AuthorIndexRow from './AuthorIndexRow';
import styles from './AuthorIndexTable.css';

class AuthorIndexTable extends Component {

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
      isEditorActive,
      showBanners,
      showTitle
    } = this.props;

    const author = items[rowIndex];

    return (
      <VirtualTableRow
        key={key}
        style={style}
      >
        <AuthorIndexItemConnector
          key={author.id}
          component={AuthorIndexRow}
          style={style}
          columns={columns}
          authorId={author.id}
          qualityProfileId={author.qualityProfileId}
          metadataProfileId={author.metadataProfileId}
          isSelected={selectedState[author.id]}
          onSelectedChange={onSelectedChange}
          isEditorActive={isEditorActive}
          showBanners={showBanners}
          showTitle={showTitle}
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
      showBanners,
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
        rowHeight={showBanners ? 70 : 38}
        overscanRowCount={2}
        rowRenderer={this.rowRenderer}
        header={
          <AuthorIndexHeaderConnector
            showBanners={showBanners}
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

AuthorIndexTable.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  columns: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string.isRequired,
  sortDirection: PropTypes.oneOf(sortDirections.all),
  showBanners: PropTypes.bool.isRequired,
  showTitle: PropTypes.string.isRequired,
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

export default AuthorIndexTable;
