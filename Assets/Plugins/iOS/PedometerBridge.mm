#import <CoreMotion/CoreMotion.h>
#import <Foundation/Foundation.h>

static CMPedometer *pedometer;
static NSInteger todaySteps = 0;

static NSDate* TodayMidnight(void) {
    NSDate *now = [NSDate date];
    NSCalendar *cal = [NSCalendar currentCalendar];
    NSDateComponents *comp = [cal components:(NSCalendarUnitYear|NSCalendarUnitMonth|NSCalendarUnitDay)
                                   fromDate:now];
    return [cal dateFromComponents:comp]; // 오늘 00:00
}

extern "C" bool iOS_Pedometer_IsSupported()
{
    return [CMPedometer isStepCountingAvailable];
}

extern "C" void iOS_Pedometer_Start()
{
    if (![CMPedometer isStepCountingAvailable()]) return;

    if (!pedometer) pedometer = [CMPedometer new];

    NSDate *midnight = TodayMidnight();

    // 기존 업데이트 중지 후 재시작(중복 방지)
    [pedometer stopPedometerUpdates];

    // 실시간 스트리밍(오늘 0시부터)
    [pedometer startPedometerUpdatesFromDate:midnight
                                 withHandler:^(CMPedometerData * _Nullable data, NSError * _Nullable error) {
        if (error || !data) return;
        // 콜백은 메인스레드가 아닐 수 있어요. 간단히 정수만 갱신.
        todaySteps = data.numberOfSteps.integerValue;
    }];

    // 시작 직후 한 번 현재까지 합계를 쿼리해 초기값 세팅
    [pedometer queryPedometerDataFromDate:midnight
                                   toDate:[NSDate date]
                              withHandler:^(CMPedometerData * _Nullable data, NSError * _Nullable error) {
        if (!error && data) {
            todaySteps = data.numberOfSteps.integerValue;
        }
    }];
}

extern "C" void iOS_Pedometer_Stop()
{
    if (pedometer) [pedometer stopPedometerUpdates];
}

extern "C" int iOS_Pedometer_GetTodaySteps()
{
    return (int)todaySteps;
}