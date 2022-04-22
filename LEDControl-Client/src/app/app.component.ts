import {Component, OnInit} from '@angular/core';
import {BreakpointObserver, Breakpoints, BreakpointState} from "@angular/cdk/layout";
import {EventService} from "./services/event.service";

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit{
  isMobileLayout: boolean;
  constructor(private breakpointObserver: BreakpointObserver, private eventService: EventService) {
  }

  ngOnInit(): void {
    this.eventService.mobileLayout.subscribe(p => this.isMobileLayout = p);
    this.initBreakpointObserver();
  }

  initBreakpointObserver(){
  this.breakpointObserver.observe(['(min-width: 900px)'])
      .subscribe((state: BreakpointState) => {
        if (state.matches) {
          this.eventService.mobileLayout.next(false);
        } else {
          this.eventService.mobileLayout.next(true);
        }
      });
  }
}
