import React from 'react';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import translate from 'Utilities/String/translate';

function AuthorMonitorNewItemsOptionsPopoverContent() {
  return (
    <DescriptionList>
      <DescriptionListItem
        title={translate('AllBooks')}
        data="Monitor all new books"
      />

      <DescriptionListItem
        title={translate('NewBooks')}
        data="Monitor new books released after the newest existing book"
      />

      <DescriptionListItem
        title={translate('None')}
        data="Don't monitor any new books"
      />
    </DescriptionList>
  );
}

export default AuthorMonitorNewItemsOptionsPopoverContent;
