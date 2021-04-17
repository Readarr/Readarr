import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function AuthorMonitoringOptionsPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('AllBooks')}
        data="Monitor all books"
      />

      <DescriptionListItem
        title={translate('FutureBooks')}
        data="Monitor books that have not released yet"
      />

      <DescriptionListItem
        title={translate('MissingBooks')}
        data="Monitor books that do not have files or have not released yet"
      />

      <DescriptionListItem
        title={translate('ExistingBooks')}
        data="Monitor books that have files or have not released yet"
      />

      <DescriptionListItem
        title={translate('FirstBook')}
        data="Monitor the first book. All other books will be ignored"
      />

      <DescriptionListItem
        title={translate('LatestBook')}
        data="Monitor the latest book and future books"
      />

      <DescriptionListItem
        title={translate('None')}
        data="No books will be monitored"
      />
    </DescriptionList>
  );
}

export default AuthorMonitoringOptionsPopoverContent;
