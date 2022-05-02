import {Component, OnInit, ViewChild} from '@angular/core';
import {EventService} from "../services/event.service";
import {BaseChartDirective} from "ng2-charts";
import {ChartDataset, ChartOptions } from 'chart.js';

@Component({
  selector: 'app-music',
  templateUrl: './music.component.html',
  styleUrls: ['./music.component.scss']
})
export class MusicComponent implements OnInit {
  @ViewChild(BaseChartDirective, { static: true }) chart: BaseChartDirective;
  dataLength = 500;
  data: ChartDataset[] = [
    {data: [], label:"Music", cubicInterpolationMode: 'monotone', tension: 0.4, pointRadius: 0}
  ];
  public labels = [];
  options: ChartOptions = {

    scales:{
      y:{
        max : 1
      },
      x: {
        display: false
      }
    }
  };

  constructor(private eventService: EventService) {
    this.eventService.subTitle.next("Music");
    for (let i = 0; i < this.dataLength; i++){
      this.labels.push(i);
    }
  }

  ngOnInit(): void {
    this.randomData();
  }

  randomData(){
    for (let i = 0; i < this.dataLength; i++){
      this.data[0].data[i] = Math.random();
    }
    this.chart.update();
  }

}
