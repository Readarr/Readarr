import { AuthorStatus } from 'Author/Author';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';

export function getAuthorStatusDetails(status: AuthorStatus) {
  let statusDetails = {
    icon: icons.AUTHOR_CONTINUING,
    title: translate('StatusEndedContinuing'),
    message: translate('ContinuingMoreBooksAreExpected'),
  };

  if (status === 'ended') {
    statusDetails = {
      icon: icons.AUTHOR_ENDED,
      title: translate('StatusEndedEnded'),
      message: translate('ContinuingNoAdditionalBooksAreExpected'),
    };
  }

  return statusDetails;
}
