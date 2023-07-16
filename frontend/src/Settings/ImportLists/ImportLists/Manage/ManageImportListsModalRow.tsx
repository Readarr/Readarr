import React, { useCallback } from 'react';
import { useSelector } from 'react-redux';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import Column from 'Components/Table/Column';
import TableRow from 'Components/Table/TableRow';
import TagListConnector from 'Components/TagListConnector';
import { createMetadataProfileSelectorForHook } from 'Store/Selectors/createMetadataProfileSelector';
import { createQualityProfileSelectorForHook } from 'Store/Selectors/createQualityProfileSelector';
import { SelectStateInputProps } from 'typings/props';
import styles from './ManageImportListsModalRow.css';

interface ManageImportListsModalRowProps {
  id: number;
  name: string;
  rootFolderPath: string;
  qualityProfileId: number;
  metadataProfileId: number;
  implementation: string;
  tags: number[];
  enableAutomaticAdd: boolean;
  columns: Column[];
  isSelected?: boolean;
  onSelectedChange(result: SelectStateInputProps): void;
}

function ManageImportListsModalRow(props: ManageImportListsModalRowProps) {
  const {
    id,
    isSelected,
    name,
    rootFolderPath,
    qualityProfileId,
    metadataProfileId,
    implementation,
    enableAutomaticAdd,
    tags,
    onSelectedChange,
  } = props;

  const qualityProfile = useSelector(
    createQualityProfileSelectorForHook(qualityProfileId)
  );

  const metadataProfile = useSelector(
    createMetadataProfileSelectorForHook(metadataProfileId)
  );

  const onSelectedChangeWrapper = useCallback(
    (result: SelectStateInputProps) => {
      onSelectedChange({
        ...result,
      });
    },
    [onSelectedChange]
  );

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChangeWrapper}
      />

      <TableRowCell className={styles.name}>{name}</TableRowCell>

      <TableRowCell className={styles.implementation}>
        {implementation}
      </TableRowCell>

      <TableRowCell className={styles.qualityProfileId}>
        {qualityProfile?.name ?? 'None'}
      </TableRowCell>

      <TableRowCell className={styles.metadataProfileId}>
        {metadataProfile?.name ?? 'None'}
      </TableRowCell>

      <TableRowCell className={styles.rootFolderPath}>
        {rootFolderPath}
      </TableRowCell>

      <TableRowCell className={styles.enableAutomaticAdd}>
        {enableAutomaticAdd ? 'Yes' : 'No'}
      </TableRowCell>

      <TableRowCell className={styles.tags}>
        <TagListConnector tags={tags} />
      </TableRowCell>
    </TableRow>
  );
}

export default ManageImportListsModalRow;
