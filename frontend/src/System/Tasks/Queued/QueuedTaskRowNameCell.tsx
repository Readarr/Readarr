import React from 'react';
import { useSelector } from 'react-redux';
import { CommandBody } from 'Commands/Command';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import createMultiAuthorsSelector from 'Store/Selectors/createMultiAuthorsSelector';
import translate from 'Utilities/String/translate';
import styles from './QueuedTaskRowNameCell.css';

export interface QueuedTaskRowNameCellProps {
  commandName: string;
  body: CommandBody;
  clientUserAgent?: string;
}

export default function QueuedTaskRowNameCell(
  props: QueuedTaskRowNameCellProps
) {
  const { commandName, body, clientUserAgent } = props;
  const movieIds = [...(body.authorIds ?? [])];

  if (body.authorId) {
    movieIds.push(body.authorId);
  }

  const authors = useSelector(createMultiAuthorsSelector(movieIds));
  const sortedAuthors = authors.sort((a, b) =>
    a.sortName.localeCompare(b.sortName)
  );

  return (
    <TableRowCell>
      <span className={styles.commandName}>
        {commandName}
        {sortedAuthors.length ? (
          <span> - {sortedAuthors.map((a) => a.authorName).join(', ')}</span>
        ) : null}
      </span>

      {clientUserAgent ? (
        <span
          className={styles.userAgent}
          title={translate('TaskUserAgentTooltip')}
        >
          {translate('From')}: {clientUserAgent}
        </span>
      ) : null}
    </TableRowCell>
  );
}
