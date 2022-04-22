import { Component, OnInit } from '@angular/core';
import {EventService} from "../services/event.service";

@Component({
  selector: 'app-converter',
  templateUrl: './converter.component.html',
  styleUrls: ['./converter.component.scss']
})
export class ConverterComponent implements OnInit {

  constructor(private eventService: EventService) {
    this.eventService.subTitle.next("Youtube Converter");
  }

  ngOnInit(): void {
  }

}
