import { Injectable } from '@angular/core';
import {BehaviorSubject} from "rxjs";

@Injectable({
  providedIn: 'root'
})
export class EventService {
  public connectionStatus = new BehaviorSubject<boolean>(false);
  public mobileLayout = new BehaviorSubject<boolean>(true);
  public subTitle = new BehaviorSubject<string>("");

  constructor() {  }
}
