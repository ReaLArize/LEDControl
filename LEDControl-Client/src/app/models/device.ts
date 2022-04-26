export class Device {
  id: string;
  name: string;
  mode: DeviceMode;
  hostname: string;
  port: number;
}

export enum DeviceMode{
  Light,
  Pictures
}
