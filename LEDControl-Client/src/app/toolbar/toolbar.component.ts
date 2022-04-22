import { Component, OnInit } from '@angular/core';
import {EventService} from "../services/event.service";

@Component({
  selector: 'app-toolbar',
  templateUrl: './toolbar.component.html',
  styleUrls: ['./toolbar.component.scss']
})
export class ToolbarComponent implements OnInit {
  subTitle: string;
  isConnected: boolean;

  constructor(private eventService: EventService) {
    eventService.connectionStatus.subscribe(p => this.isConnected = p);
    eventService.subTitle.subscribe(p => this.subTitle = p);
  }

  ngOnInit(): void {
  }

}
