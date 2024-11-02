import AppSectionState, {
  AppSectionDeleteState,
  AppSectionItemState,
  AppSectionSaveState,
} from 'App/State/AppSectionState';
import DownloadClient from 'typings/DownloadClient';
import ImportList from 'typings/ImportList';
import Indexer from 'typings/Indexer';
import IndexerFlag from 'typings/IndexerFlag';
import Notification from 'typings/Notification';
import General from 'typings/Settings/General';
import UiSettings from 'typings/Settings/UiSettings';

export interface DownloadClientAppState
  extends AppSectionState<DownloadClient>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export type GeneralAppState = AppSectionItemState<General>;

export interface ImportListAppState
  extends AppSectionState<ImportList>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface IndexerAppState
  extends AppSectionState<Indexer>,
    AppSectionDeleteState,
    AppSectionSaveState {}

export interface NotificationAppState
  extends AppSectionState<Notification>,
    AppSectionDeleteState {}

export type IndexerFlagSettingsAppState = AppSectionState<IndexerFlag>;
export type UiSettingsAppState = AppSectionState<UiSettings>;

interface SettingsAppState {
  downloadClients: DownloadClientAppState;
  general: GeneralAppState;
  importLists: ImportListAppState;
  indexerFlags: IndexerFlagSettingsAppState;
  indexers: IndexerAppState;
  notifications: NotificationAppState;
  ui: UiSettingsAppState;
}

export default SettingsAppState;
