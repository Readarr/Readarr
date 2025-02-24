import { IconDefinition } from '@fortawesome/free-regular-svg-icons';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

interface AuthorStatus {
  icon: IconDefinition;
  title: string;
  message: string;
}

export function getAuthorStatusDetails(status): AuthorStatus {
  let statusDetails = {
    icon: icons.AUTHOR_CONTINUING,
    title: translate('StatusEndedContinuing'),
    message: translate('ContinuingMoreBooksAreExpected'),
  };

  if (status === 'deleted') {
    statusDetails = {
      icon: icons.AUTHOR_DELETED,
      title: translate('StatusEndedDeceased'),
      message: translate('NotContinuingAuthorDeceased'),
    };
  } else if (status === 'ended') {
    statusDetails = {
      icon: icons.AUTHOR_ENDED,
      title: translate('StatusEndedEnded'),
      message: translate('ContinuingNoAdditionalBooksAreExpected'),
    };
  }

  return statusDetails;
}
