import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Measure from 'Components/Measure';
import { isMobile as isMobileUtil } from 'Utilities/browser';
import styles from './SwipeHeader.css';

function cursorPosition(event) {
  return event.touches ? event.touches[0].clientX : event.clientX;
}

class SwipeHeader extends Component {

  //
  // Lifecycle

  constructor(props) {
    super(props);

    this._isMobile = isMobileUtil();

    this.state = {
      containerWidth: 0,
      touching: null,
      translate: 0,
      stage: 'init',
      url: null
    };
  }

  componentWillUnmount() {
    this.removeEventListeners();
  }

  //
  // Listeners

  onMouseDown = (e) => {
    if (!this.props.isSmallScreen || !this._isMobile || this.state.touching) {
      return;
    }

    this.startTouchPosition = cursorPosition(e);
    this.initTranslate = this.state.translate;

    this.setState({
      stage: null,
      touching: true }, () => {
      this.addEventListeners();
    });
  };

  addEventListeners = () => {
    window.addEventListener('mousemove', this.onMouseMove);
    window.addEventListener('touchmove', this.onMouseMove);
    window.addEventListener('mouseup', this.onMouseUp);
    window.addEventListener('touchend', this.onMouseUp);
  };

  removeEventListeners = () => {
    window.removeEventListener('mousemove', this.onMouseMove);
    window.removeEventListener('touchmove', this.onMouseMove);
    window.removeEventListener('mouseup', this.onMouseUp);
    window.removeEventListener('touchend', this.onMouseUp);
  };

  onMouseMove = (e) => {
    const {
      touching,
      containerWidth
    } = this.state;

    if (!touching) {
      return;
    }

    const translate = Math.max(Math.min(cursorPosition(e) - this.startTouchPosition + this.initTranslate, containerWidth), -1 * containerWidth);

    this.setState({ translate });
  };

  onMouseUp = () => {
    this.startTouchPosition = null;

    const {
      nextLink,
      prevLink,
      navWidth
    } = this.props;

    const {
      containerWidth,
      translate
    } = this.state;

    const newState = {
      touching: false
    };

    const acceptableMove = navWidth * 0.7;
    const showNav = Math.abs(translate) >= acceptableMove;
    const navWithoutConfirm = Math.abs(translate) >= containerWidth * 0.5;

    if (navWithoutConfirm) {
      newState.translate = Math.sign(translate) * containerWidth;
    }

    if (!showNav) {
      newState.translate = 0;
      newState.stage = null;
    }

    if (showNav && !navWithoutConfirm) {
      newState.translate = Math.sign(translate) * navWidth;
      newState.stage = 'showNav';
    }

    this.setState(newState, () => {
      if (navWithoutConfirm) {
        this.onNavClick(translate < 0 ? nextLink : prevLink, Math.abs(translate) === containerWidth);
      }
    });

    this.removeEventListeners();
  };

  onNavClick = (url, callTransition) => {
    const {
      containerWidth,
      translate
    } = this.state;

    this.setState({
      stage: 'navigating',
      translate: Math.sign(translate) * containerWidth,
      url
    }, () => {
      if (callTransition) {
        this.onTransitionEnd();
      }
    });
  };

  onTransitionEnd = (e) => {
    const {
      stage,
      url
    } = this.state;

    if (stage === 'navigating') {
      this.setState({
        stage: 'navigated',
        translate: 0,
        url: null
      }, () => {
        this.props.onGoTo(url);
        this.setState({ stage: null });
      });
    }
  };

  onNext = () => {
    this.onNavClick(this.props.nextLink);
  };

  onPrev = () => {
    this.onNavClick(this.props.prevLink);
  };

  onContainerMeasure = ({ width }) => {
    this.setState({ containerWidth: width });
  };

  //
  // Render

  render() {
    const {
      transitionDuration,
      className,
      children,
      prevComponent,
      currentComponent,
      nextComponent,
      isSmallScreen
    } = this.props;

    const {
      containerWidth,
      translate,
      touching,
      stage
    } = this.state;

    const allowSwipe = isSmallScreen && this._isMobile;

    const useTransition = !touching && stage !== 'navigated' && stage !== 'init';

    const style = {
      width: '100%',
      '--translate': 0
    };

    if (allowSwipe) {
      style.width = '300%';
      style['--translate'] = `${translate - containerWidth}px`;
      style['--transition'] = useTransition ? `transform ${transitionDuration}ms ease-out` : undefined;
    }

    return (
      <Measure
        className={className}
        onMeasure={this.onContainerMeasure}
      >
        {children}

        <div
          className={styles.content}
          style={style}
          onMouseDown={this.onMouseDown}
          onTouchStart={this.onMouseDown}
          onTransitionEnd={this.onTransitionEnd}
        >
          {allowSwipe ? prevComponent(containerWidth) : null}
          {currentComponent(containerWidth)}
          {allowSwipe ? nextComponent(containerWidth) : null}
        </div>
      </Measure>
    );
  }
}

SwipeHeader.propTypes = {
  transitionDuration: PropTypes.number.isRequired,
  navWidth: PropTypes.number.isRequired,
  nextLink: PropTypes.string,
  prevLink: PropTypes.string,
  nextComponent: PropTypes.func.isRequired,
  currentComponent: PropTypes.func.isRequired,
  prevComponent: PropTypes.func.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  className: PropTypes.string,
  onGoTo: PropTypes.func.isRequired,
  children: PropTypes.node.isRequired
};

SwipeHeader.defaultProps = {
  transitionDuration: 250,
  navWidth: 75
};

export default SwipeHeader;
