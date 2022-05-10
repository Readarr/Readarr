import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function AuthorMonitorNewItemsOptionsPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('AllBooks')}
        data={translate('DataNewAllBooks')}
      />

      <DescriptionListItem
        title={translate('NewBooks')}
        data={translate('DataNewBooks')}
      />

      <DescriptionListItem
        title={translate('None')}
        data={translate('DataNewNone')}
      />
    </DescriptionList>
  );
}

export default AuthorMonitorNewItemsOptionsPopoverContent;
