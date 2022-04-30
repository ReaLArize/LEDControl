import { Component, OnInit } from '@angular/core';
import {EventService} from "../services/event.service";

@Component({
  selector: 'app-music',
  templateUrl: './music.component.html',
  styleUrls: ['./music.component.scss']
})
export class MusicComponent implements OnInit {

  constructor(private eventService: EventService) {
    this.eventService.subTitle.next("Music");
  }

  ngOnInit(): void {
  }

}
