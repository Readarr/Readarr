import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import Autosuggest from 'react-autosuggest';
import Icon from 'Components/Icon';
import keyboardShortcuts, { shortcuts } from 'Components/keyboardShortcuts';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import AuthorSearchResult from './AuthorSearchResult';
import BookSearchResult from './BookSearchResult';
import FuseWorker from './fuse.worker';
import styles from './AuthorSearchInput.css';

const ADD_NEW_TYPE = 'addNew';

class AuthorSearchInput extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this._autosuggest = null;
    this._worker = null;

    this.state = {
      value: '',
      suggestions: []
    };
  }

  componentDidMount() {
    this.props.bindShortcut(shortcuts.AUTHOR_SEARCH_INPUT.key, this.focusInput);
  }

  componentWillUnmount() {
    if (this._worker) {
      this._worker.removeEventListener('message', this.onSuggestionsReceived, false);
      this._worker.terminate();
      this._worker = null;
    }
  }

  getWorker() {
    if (!this._worker) {
      this._worker = new FuseWorker();
      this._worker.addEventListener('message', this.onSuggestionsReceived, false);
    }

    return this._worker;
  }

  //
  // Control

  setAutosuggestRef = (ref) => {
    this._autosuggest = ref;
  };

  focusInput = (event) => {
    event.preventDefault();
    this._autosuggest.input.focus();
  };

  getSectionSuggestions(section) {
    return section.suggestions;
  }

  renderSectionTitle(section) {
    return (
      <div className={styles.sectionTitle}>
        {section.title}

        {
          section.loading &&
            <LoadingIndicator
              className={styles.loading}
              rippleClassName={styles.ripple}
              size={20}
            />
        }
      </div>
    );
  }

  getSuggestionValue({ title }) {
    return title;
  }

  renderSuggestion(item, { query }) {
    if (item.type === ADD_NEW_TYPE) {
      return (
        <div className={styles.addNewAuthorSuggestion}>
          Search for {query}
        </div>
      );
    }

    if (item.item.type === 'author') {
      return (
        <AuthorSearchResult
          {...item.item}
          match={item.matches[0]}
        />
      );
    }

    if (item.item.type === 'book') {
      return (
        <BookSearchResult
          {...item.item}
          match={item.matches[0]}
        />
      );
    }
  }

  goToItem(item) {
    const {
      onGoToAuthor,
      onGoToBook
    } = this.props;

    this.setState({ value: '' });

    const {
      type,
      titleSlug
    } = item.item;

    if (type === 'author') {
      onGoToAuthor(titleSlug);
    } else if (type === 'book') {
      onGoToBook(titleSlug);
    }
  }

  reset() {
    this.setState({
      value: '',
      suggestions: [],
      loading: false
    });
  }

  //
  // Listeners

  onChange = (event, { newValue, method }) => {
    if (method === 'up' || method === 'down') {
      return;
    }

    this.setState({ value: newValue });
  };

  onKeyDown = (event) => {
    if (event.shiftKey || event.altKey || event.ctrlKey) {
      return;
    }

    if (event.key === 'Escape') {
      this.reset();
      return;
    }

    if (event.key !== 'Tab' && event.key !== 'Enter') {
      return;
    }

    const {
      suggestions,
      value
    } = this.state;

    const {
      highlightedSectionIndex,
      highlightedSuggestionIndex
    } = this._autosuggest.state;

    if (!suggestions.length || highlightedSectionIndex) {
      this.props.onGoToAddNewAuthor(value);
      this._autosuggest.input.blur();
      this.reset();

      return;
    }

    // If an suggestion is not selected go to the first author,
    // otherwise go to the selected author.

    if (highlightedSuggestionIndex == null) {
      this.goToItem(suggestions[0]);
    } else {
      this.goToItem(suggestions[highlightedSuggestionIndex]);
    }

    this._autosuggest.input.blur();
    this.reset();
  };

  onBlur = () => {
    this.reset();
  };

  onSuggestionsFetchRequested = ({ value }) => {
    if (!this.state.loading) {
      this.setState({
        loading: true
      });
    }

    this.requestSuggestions(value);
  };

  requestSuggestions = _.debounce((value) => {
    if (!this.state.loading) {
      return;
    }

    const requestLoading = this.state.requestLoading;

    this.setState({
      requestValue: value,
      requestLoading: true
    });

    if (!requestLoading) {
      const payload = {
        value,
        items: this.props.items
      };

      this.getWorker().postMessage(payload);
    }
  }, 250);

  onSuggestionsReceived = (message) => {
    const {
      value,
      suggestions
    } = message.data;

    if (!this.state.loading) {
      this.setState({
        requestValue: null,
        requestLoading: false
      });
    } else if (value === this.state.requestValue) {
      this.setState({
        suggestions,
        requestValue: null,
        requestLoading: false,
        loading: false
      });
    } else {
      this.setState({
        suggestions,
        requestLoading: true
      });

      const payload = {
        value: this.state.requestValue,
        items: this.props.items
      };

      this.getWorker().postMessage(payload);
    }
  };

  onSuggestionsClearRequested = () => {
    this.setState({
      suggestions: [],
      loading: false
    });
  };

  onSuggestionSelected = (event, { suggestion }) => {
    if (suggestion.type === ADD_NEW_TYPE) {
      this.props.onGoToAddNewAuthor(this.state.value);
    } else {
      this.goToItem(suggestion);
    }
  };

  //
  // Render

  render() {
    const {
      value,
      loading,
      suggestions
    } = this.state;

    const suggestionGroups = [];

    if (suggestions.length || loading) {
      suggestionGroups.push({
        title: translate('ExistingItems'),
        loading,
        suggestions
      });
    }

    suggestionGroups.push({
      title: translate('AddNewItem'),
      suggestions: [
        {
          type: ADD_NEW_TYPE,
          title: value
        }
      ]
    });

    const inputProps = {
      ref: this.setInputRef,
      className: styles.input,
      name: 'authorSearch',
      value,
      placeholder: translate('Search'),
      autoComplete: 'off',
      spellCheck: false,
      onChange: this.onChange,
      onKeyDown: this.onKeyDown,
      onBlur: this.onBlur,
      onFocus: this.onFocus
    };

    const theme = {
      container: styles.container,
      containerOpen: styles.containerOpen,
      suggestionsContainer: styles.authorContainer,
      suggestionsList: styles.list,
      suggestion: styles.listItem,
      suggestionHighlighted: styles.highlighted
    };

    return (
      <div className={styles.wrapper}>
        <Icon name={icons.SEARCH} />

        <Autosuggest
          ref={this.setAutosuggestRef}
          id={name}
          inputProps={inputProps}
          theme={theme}
          focusInputOnSuggestionClick={false}
          multiSection={true}
          suggestions={suggestionGroups}
          getSectionSuggestions={this.getSectionSuggestions}
          renderSectionTitle={this.renderSectionTitle}
          getSuggestionValue={this.getSuggestionValue}
          renderSuggestion={this.renderSuggestion}
          onSuggestionSelected={this.onSuggestionSelected}
          onSuggestionsFetchRequested={this.onSuggestionsFetchRequested}
          onSuggestionsClearRequested={this.onSuggestionsClearRequested}
        />
      </div>
    );
  }
}

AuthorSearchInput.propTypes = {
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  onGoToAuthor: PropTypes.func.isRequired,
  onGoToBook: PropTypes.func.isRequired,
  onGoToAddNewAuthor: PropTypes.func.isRequired,
  bindShortcut: PropTypes.func.isRequired
};

export default keyboardShortcuts(AuthorSearchInput);
