# Khala.TransientFaultHanding

일시적 실패에 대한 일반화된 재시도 정책 구현체입니다.

```csharp
public Task<HttpResponseMessage> SendHttpRequestWithRetryPolicy(
    HttpClient httpClient,
    HttpRequestMessage request,
    CancellationToken cancellationToken)
{
    // 최대 재시도 횟수
    int maximumRetryCount = 5;

    // 일시적 실패 감지 전략
    var transientFaultDetectionStrategy =
        new DelegatingTransientFaultDetectionStrategy<HttpResponseMessage>(
            exception => true,
            response => response.IsSuccessStatusCode == false);

    // 재시도 지연 전략
    var retryIntervalStrategy =
        new LinearRetryIntervalStrategy(
            initialInterval: TimeSpan.Zero,
            increment: TimeSpan.FromMilliseconds(500),
            maximumInterval: TimeSpan.MaxValue,
            immediateFirstRetry: false);

    // 재시도 정책
    var retryPolicy = new RetryPolicy<HttpResponseMessage>(
        maximumRetryCount,
        transientFaultDetectionStrategy,
        retryIntervalStrategy);

    // 재시도 정책을 통한 HTTP 요청
    return retryPolicy.Run(
        httpClient.SendAsync,
        request,
        cancellationToken);
}
```

## 설치

```text
PM> Install-Package Khala.TransientFaultHandling
```

## 재시도 정책 구성

재시도 정책은 다음 3가지 요소로 구성됩니다.

|요소|설명|
|--|--|
|최대 재시도 횟수|연산이 실패할 때 최대로 재시도 할 횟수입니다.|
|일시적 실패 감지 전략|연산 실행중 발생한 예외나 반환 값이 일시적 실패인지 판단합니다.|
|재시도 지연 전략|일시적 실패 후 다시 연산 재실행을 지연시킬 시간을 결정합니다.|

## 반환 값을 가지지 않는 비동기 연산에 대한 재시도 정책

`Khala.TransientFaultHandling.RetryPolicy` 클래스가 반환 값을 가지지 않는 비동기 연산에 대한 재시도 정책을 구현합니다.

```csharp
public class RetryPolicy
{
    public RetryPolicy(
        int maximumRetryCount,
        TransientFaultDetectionStrategy transientFaultDetectionStrategy,
        RetryIntervalStrategy retryIntervalStrategy);

    public virtual Task Run(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);

    public virtual Task Run<T>(
        Func<T, CancellationToken, Task> operation,
        T arg,
        CancellationToken cancellationToken);
}
```

## 반환 값을 가지는 비동기 연산에 대한 재시도 정책

`Khala.TransientFaultHandling.RetryPolicy<TResult>` 클래스가 반환 값을 가지는 비동기 연산에 대한 재시도 정책을 구현합니다.

```csharp
public class RetryPolicy<TResult>
{
    public RetryPolicy(
        int maximumRetryCount,
        TransientFaultDetectionStrategy<TResult> transientFaultDetectionStrategy,
        RetryIntervalStrategy retryIntervalStrategy);

    public virtual Task<TResult> Run(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);

    public virtual Task<TResult> Run<T>(
        Func<T, CancellationToken, Task<TResult>> operation,
        T arg,
        CancellationToken cancellationToken);
}
```

## License

```
MIT License

Copyright (c) 2017 Gyuwon Yi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
