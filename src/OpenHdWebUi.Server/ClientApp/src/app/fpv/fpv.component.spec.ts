import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FpvComponent } from './fpv.component';

describe('FpvComponent', () => {
  let component: FpvComponent;
  let fixture: ComponentFixture<FpvComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ FpvComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(FpvComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
