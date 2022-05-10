import {Component, Inject, OnInit} from '@angular/core';
import {MAT_DIALOG_DATA, MatDialogRef} from "@angular/material/dialog";
import {Device} from "../../models/device";

@Component({
  selector: 'app-add-device',
  templateUrl: './add-device.component.html',
  styleUrls: ['./add-device.component.scss']
})
export class AddDeviceComponent implements OnInit {
  device: Device;

  constructor(private dialogRef: MatDialogRef<AddDeviceComponent>, @Inject(MAT_DIALOG_DATA) private data: Device) {
    this.device = this.data;
  }

  ngOnInit(): void {
  }

  cancel(){
    this.dialogRef.close();
  }
}
