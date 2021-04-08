import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ReactMeasure from 'react-measure';

class Measure extends Component {

  //
  // Lifecycle

  componentWillUnmount() {
    this.onMeasure.cancel();
  }

  //
  // Listeners

  onMeasure = _.debounce((payload) => {
    this.props.onMeasure(payload.bounds);
  }, 250, { leading: true, trailing: true })

  //
  // Render

  render() {
    const {
      className,
      style,
      onMeasure,
      children,
      ...otherProps
    } = this.props;

    return (
      <ReactMeasure
        bounds={true}
        onResize={this.onMeasure}
        {...otherProps}
      >
        {({ measureRef }) => (
          <div
            ref={measureRef}
            className={className}
            style={style}
          >
            {children}
          </div>
        )}
      </ReactMeasure>
    );
  }
}

Measure.propTypes = {
  className: PropTypes.string,
  style: PropTypes.oneOfType([PropTypes.object, PropTypes.array]),
  onMeasure: PropTypes.func.isRequired,
  children: PropTypes.node
};

export default Measure;
