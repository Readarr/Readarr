import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { Grid, WindowScroller } from 'react-virtualized';
import AuthorIndexItemConnector from 'Author/Index/AuthorIndexItemConnector';
import Measure from 'Components/Measure';
import dimensions from 'Styles/Variables/dimensions';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import hasDifferentItemsOrOrder from 'Utilities/Object/hasDifferentItemsOrOrder';
import AuthorIndexOverview from './AuthorIndexOverview';
import styles from './AuthorIndexOverviews.css';

// Poster container dimensions
const columnPadding = parseInt(dimensions.authorIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.authorIndexColumnPaddingSmallScreen);
const progressBarHeight = parseInt(dimensions.progressBarSmallHeight);
const detailedProgressBarHeight = parseInt(dimensions.progressBarMediumHeight);

function calculatePosterWidth(posterSize, isSmallScreen) {
  const maxiumPosterWidth = isSmallScreen ? 192 : 202;

  if (posterSize === 'large') {
    return maxiumPosterWidth;
  }

  if (posterSize === 'medium') {
    return Math.floor(maxiumPosterWidth * 0.75);
  }

  return Math.floor(maxiumPosterWidth * 0.5);
}

function calculateRowHeight(posterHeight, sortKey, isSmallScreen, overviewOptions) {
  const {
    detailedProgressBar
  } = overviewOptions;

  const heights = [
    posterHeight,
    detailedProgressBar ? detailedProgressBarHeight : progressBarHeight,
    isSmallScreen ? columnPaddingSmallScreen : columnPadding
  ];

  return heights.reduce((acc, height) => acc + height, 0);
}

function calculatePosterHeight(posterWidth) {
  return posterWidth;
}

class AuthorIndexOverviews extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      width: 0,
      columnCount: 1,
      posterWidth: 238,
      posterHeight: 238,
      rowHeight: calculateRowHeight(238, null, props.isSmallScreen, {})
    };

    this._grid = null;
  }

  componentDidUpdate(prevProps, prevState) {
    const {
      items,
      sortKey,
      overviewOptions,
      jumpToCharacter
    } = this.props;

    const {
      width,
      rowHeight
    } = this.state;

    if (prevProps.sortKey !== sortKey ||
        prevProps.overviewOptions !== overviewOptions) {
      this.calculateGrid();
    }

    if (this._grid &&
        (prevState.width !== width ||
            prevState.rowHeight !== rowHeight ||
            hasDifferentItemsOrOrder(prevProps.items, items))) {
      // recomputeGridSize also forces Grid to discard its cache of rendered cells
      this._grid.recomputeGridSize();
    }

    if (jumpToCharacter != null && jumpToCharacter !== prevProps.jumpToCharacter) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (this._grid && index != null) {

        this._grid.scrollToCell({
          rowIndex: index,
          columnIndex: 0
        });
      }
    }
  }

  //
  // Control

  setGridRef = (ref) => {
    this._grid = ref;
  }

  calculateGrid = (width = this.state.width, isSmallScreen) => {
    const {
      sortKey,
      overviewOptions
    } = this.props;

    const posterWidth = calculatePosterWidth(overviewOptions.size, isSmallScreen);
    const posterHeight = calculatePosterHeight(posterWidth);
    const rowHeight = calculateRowHeight(posterHeight, sortKey, isSmallScreen, overviewOptions);

    this.setState({
      width,
      posterWidth,
      posterHeight,
      rowHeight
    });
  }

  cellRenderer = ({ key, rowIndex, style }) => {
    const {
      items,
      sortKey,
      overviewOptions,
      showRelativeDates,
      shortDateFormat,
      longDateFormat,
      timeFormat,
      isSmallScreen
    } = this.props;

    const {
      posterWidth,
      posterHeight,
      rowHeight
    } = this.state;

    const author = items[rowIndex];

    if (!author) {
      return null;
    }

    return (
      <div
        key={key}
        style={style}
      >
        <AuthorIndexItemConnector
          key={author.id}
          component={AuthorIndexOverview}
          sortKey={sortKey}
          posterWidth={posterWidth}
          posterHeight={posterHeight}
          rowHeight={rowHeight}
          overviewOptions={overviewOptions}
          showRelativeDates={showRelativeDates}
          shortDateFormat={shortDateFormat}
          longDateFormat={longDateFormat}
          timeFormat={timeFormat}
          isSmallScreen={isSmallScreen}
          authorId={author.id}
          qualityProfileId={author.qualityProfileId}
          metadataProfileId={author.metadataProfileId}
        />
      </div>
    );
  }

  //
  // Listeners

  onMeasure = ({ width }) => {
    this.calculateGrid(width, this.props.isSmallScreen);
  }

  //
  // Render

  render() {
    const {
      items,
      isSmallScreen,
      scroller
    } = this.props;

    const {
      width,
      rowHeight
    } = this.state;

    return (
      <Measure
        onMeasure={this.onMeasure}
      >
        <WindowScroller
          scrollElement={isSmallScreen ? undefined : scroller}
        >
          {({ height, registerChild, onChildScroll, scrollTop }) => {
            if (!height) {
              return <div />;
            }

            return (
              <div ref={registerChild}>
                <Grid
                  ref={this.setGridRef}
                  className={styles.grid}
                  autoHeight={true}
                  height={height}
                  columnCount={1}
                  columnWidth={width}
                  rowCount={items.length}
                  rowHeight={rowHeight}
                  width={width}
                  onScroll={onChildScroll}
                  scrollTop={scrollTop}
                  overscanRowCount={2}
                  cellRenderer={this.cellRenderer}
                  onSectionRendered={this.onSectionRendered}
                  scrollToAlignment={'start'}
                  isScrollingOptOut={true}
                />
              </div>
            );
          }
          }
        </WindowScroller>
      </Measure>
    );
  }
}

AuthorIndexOverviews.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  sortKey: PropTypes.string,
  overviewOptions: PropTypes.object.isRequired,
  scrollTop: PropTypes.number.isRequired,
  jumpToCharacter: PropTypes.string,
  scroller: PropTypes.instanceOf(Element).isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  timeFormat: PropTypes.string.isRequired
};

export default AuthorIndexOverviews;
