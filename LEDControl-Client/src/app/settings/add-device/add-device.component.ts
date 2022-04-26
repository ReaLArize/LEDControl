import {Component, OnInit} from '@angular/core';
import {MatDialogRef} from "@angular/material/dialog";
import {Device} from "../../models/device";

@Component({
  selector: 'app-add-device',
  templateUrl: './add-device.component.html',
  styleUrls: ['./add-device.component.scss']
})
export class AddDeviceComponent implements OnInit {
  device: Device = new Device();

  constructor(private dialogRef: MatDialogRef<AddDeviceComponent>) { }

  ngOnInit(): void {
  }

  cancel(){
    this.dialogRef.close();
  }
}
