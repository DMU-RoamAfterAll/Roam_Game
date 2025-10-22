// PedometerBridge.mm
#import <CoreMotion/CoreMotion.h>
#import <Foundation/Foundation.h>

static CMPedometer *pedometer = nil;
static NSInteger todaySteps = 0;

static NSDate *TodayMidnight(void) {
    NSDate *now = [NSDate date];
    NSCalendar *cal = [NSCalendar currentCalendar];
    NSDateComponents *comp =
        [cal components:(NSCalendarUnitYear | NSCalendarUnitMonth | NSCalendarUnitDay)
               fromDate:now];
    return [cal dateFromComponents:comp]; // 오늘 00:00
}

#ifdef __cplusplus
extern "C" {
#endif

bool iOS_Pedometer_IsSupported(void)
{
    return [CMPedometer isStepCountingAvailable];   // ✅ 괄호 제거
}

void iOS_Pedometer_Start(void)
{
    if (![CMPedometer isStepCountingAvailable]) return;  // ✅ 괄호 제거

    if (!pedometer) pedometer = [CMPedometer new];

    NSDate *midnight = TodayMidnight();

    [pedometer stopPedometerUpdates];

    [pedometer startPedometerUpdatesFromDate:midnight
                                 withHandler:^(CMPedometerData * _Nullable data, NSError * _Nullable error) {
        if (error || !data) return;
        todaySteps = data.numberOfSteps.integerValue;
    }];

    [pedometer queryPedometerDataFromDate:midnight
                                   toDate:[NSDate date]
                              withHandler:^(CMPedometerData * _Nullable data, NSError * _Nullable error) {
        if (!error && data) {
            todaySteps = data.numberOfSteps.integerValue;
        }
    }];
}

void iOS_Pedometer_Stop(void)
{
    if (pedometer) [pedometer stopPedometerUpdates];
}

int iOS_Pedometer_GetTodaySteps(void)
{
    return (int)todaySteps;
}

#ifdef __cplusplus
}
#endif