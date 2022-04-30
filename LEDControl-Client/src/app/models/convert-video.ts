export class ConvertVideo {
  id: string;
  link: string;
  title: string;
  hint: string;
  convertProgress: number;
  downloadProgress: number;
  convertStatus: ConvertStatus;
  conversionPreset: ConversionPreset;
}

export enum ConvertStatus{
  Waiting,
  Processing,
  Done,
  Failed
}

export enum ConversionPreset {
  VerySlow,
  Slower,
  Slow,
  Medium,
  Fast,
  Faster,
  VeryFast,
  SuperFast,
  UltraFast,
}
