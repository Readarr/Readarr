import AuthorsAppState from './AuthorsAppState';
import CommandAppState from './CommandAppState';
import SettingsAppState from './SettingsAppState';
import SystemAppState from './SystemAppState';
import TagsAppState from './TagsAppState';

interface FilterBuilderPropOption {
  id: string;
  name: string;
}

export interface FilterBuilderProp<T> {
  name: string;
  label: string;
  type: string;
  valueType?: string;
  optionsSelector?: (items: T[]) => FilterBuilderPropOption[];
}

export interface PropertyFilter {
  key: string;
  value: boolean | string | number | string[] | number[];
  type: string;
}

export interface Filter {
  key: string;
  label: string;
  filers: PropertyFilter[];
}

export interface CustomFilter {
  id: number;
  type: string;
  label: string;
  filers: PropertyFilter[];
}

export interface AppSectionState {
  isConnected: boolean;
  isReconnecting: boolean;
  version: string;
  prevVersion?: string;
  dimensions: {
    isSmallScreen: boolean;
    width: number;
    height: number;
  };
}

interface AppState {
  app: AppSectionState;
  authors: AuthorsAppState;
  commands: CommandAppState;
  settings: SettingsAppState;
  system: SystemAppState;
  tags: TagsAppState;
}

export default AppState;
