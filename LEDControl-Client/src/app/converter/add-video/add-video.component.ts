import { Component, OnInit } from '@angular/core';
import {ConvertVideo} from "../../models/convert-video";
import {MatDialogRef} from "@angular/material/dialog";

@Component({
  selector: 'app-add-video',
  templateUrl: './add-video.component.html',
  styleUrls: ['./add-video.component.scss']
})
export class AddVideoComponent implements OnInit {

  video: ConvertVideo = new ConvertVideo();
  constructor(private dialogRef: MatDialogRef<AddVideoComponent>) {

  }

  ngOnInit(): void {
  }

  cancel(){
    this.dialogRef.close();
  }
}
