import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Measure from './Measure';
import styles from './Marquee.css';

const SPEED = 50; // pixels per second

class Marquee extends Component {

  //
  // Lifecycle

  constructor(props) {
    super(props);

    this.state = {
      containerWidth: 0,
      overflowWidth: 0,
      animationState: null,
      key: 0
    };
  }

  componentDidUpdate(prevProps) {
    if (this.props.text !== prevProps.text) {
      // reset the component, set a new key to force re-render so new text isn't in old position
      this.setState({
        overflowWidth: 0,
        animationState: null,
        key: this.state.key + 1
      });
      return;
    }

    const containerWidth = this.state.containerWidth;
    const node = this.text;

    if (containerWidth && node) {
      const textWidth = node.offsetWidth;
      // eslint-disable-next-line no-bitwise
      const overflowWidth = (textWidth - containerWidth + 10) | 0; // 10 margin, round towards 0

      if (overflowWidth !== this.state.overflowWidth) {
        const triggerUpdate = overflowWidth > 0 && this.state.overflowWidth === 0;

        this.setState({ overflowWidth }, () => {
          if (triggerUpdate) {
            this.onHandleMouseEnter();
          }
        });
      }
    }
  }

  //
  // Listeners

  onHandleMouseEnter = () => {
    const {
      animationState,
      overflowWidth
    } = this.state;

    if (animationState === null && overflowWidth > 0) {
      this.setState({ animationState: 'toLeft' });
    }
  };

  onTransitionEnd = (payload) => {
    const {
      animationState
    } = this.state;

    if (animationState === 'toLeft') {
      this.setState({ animationState: 'toRight' });
    }

    if (animationState === 'toRight') {
      this.setState({ animationState: null });
    }
  };

  onContainerMeasure = ({ width }) => {
    this.setState({ containerWidth: width });
  };

  //
  // Render

  render() {
    const {
      text
    } = this.props;

    const {
      key,
      overflowWidth,
      animationState
    } = this.state;

    const moveDist = -overflowWidth - 10;
    const duration = -moveDist / SPEED;

    const style = {
      '--duration': `${duration}s`,
      '--distance': `${moveDist}px`
    };

    return (
      <Measure
        key={key}
        className={styles.container}
        onMeasure={this.onContainerMeasure}
      >
        <div
          className={classNames(
            styles.inner,
            animationState === 'toLeft' && styles.toLeft
          )}
          style={style}
          onTransitionEnd={this.onTransitionEnd}
          onMouseEnter={this.onHandleMouseEnter}
          onTouchStart={this.onHandleMouseEnter}
        >
          <span
            ref={(el) => {
              this.text = el;
            }}
            title={text}
          >
            {text}
          </span>
        </div>
      </Measure>
    );
  }
}

Marquee.propTypes = {
  text: PropTypes.string.isRequired
};

export default Marquee;
