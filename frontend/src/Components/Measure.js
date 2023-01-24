import PropTypes from 'prop-types';
import React, { Component } from 'react';
import ReactMeasure from 'react-measure';

class Measure extends Component {

  //
  // Listeners

  onMeasure = (payload) => {
    this.props.onMeasure(payload.bounds);
  };

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
