import { IStrategy, IAsyncStrategy } from './strategy.interface';

/**
 * Strategy created from a lambda function
 */
export class LambdaStrategy<TInput = unknown, TOutput = unknown>
  implements IStrategy<TInput, TOutput>
{
  constructor(private readonly runner: (input: TInput) => TOutput) {}

  /**
   * Execute the strategy
   */
  execute(input: TInput): TOutput {
    return this.runner(input);
  }

  /**
   * Create a new strategy that applies a transformation
   */
  map<TNext>(transform: (output: TOutput) => TNext): LambdaStrategy<TInput, TNext> {
    return new LambdaStrategy((input: TInput) => transform(this.execute(input)));
  }

  /**
   * Create a new strategy that filters based on a predicate
   */
  filter(predicate: (output: TOutput) => boolean): LambdaStrategy<TInput, TOutput | undefined> {
    return new LambdaStrategy((input: TInput) => {
      const result = this.execute(input);
      return predicate(result) ? result : undefined;
    });
  }

  /**
   * Create a new strategy that applies a side effect
   */
  tap(effect: (output: TOutput) => void): LambdaStrategy<TInput, TOutput> {
    return new LambdaStrategy((input: TInput) => {
      const result = this.execute(input);
      effect(result);
      return result;
    });
  }

  /**
   * Create a new strategy with error handling
   */
  onError(handler: (error: Error) => TOutput): LambdaStrategy<TInput, TOutput> {
    return new LambdaStrategy((input: TInput) => {
      try {
        return this.execute(input);
      } catch (error) {
        return handler(error instanceof Error ? error : new Error(String(error)));
      }
    });
  }
}

/**
 * Async strategy created from a lambda function
 */
export class AsyncLambdaStrategy<TInput = unknown, TOutput = unknown>
  implements IAsyncStrategy<TInput, TOutput>
{
  constructor(private readonly runner: (input: TInput) => Promise<TOutput>) {}

  /**
   * Execute the strategy
   */
  execute(input: TInput): Promise<TOutput> {
    return this.runner(input);
  }

  /**
   * Create a new strategy that applies a transformation
   */
  map<TNext>(
    transform: (output: TOutput) => TNext | Promise<TNext>
  ): AsyncLambdaStrategy<TInput, TNext> {
    return new AsyncLambdaStrategy(async (input: TInput) => {
      const result = await this.execute(input);
      return transform(result);
    });
  }

  /**
   * Create a new strategy that filters based on a predicate
   */
  filter(
    predicate: (output: TOutput) => boolean | Promise<boolean>
  ): AsyncLambdaStrategy<TInput, TOutput | undefined> {
    return new AsyncLambdaStrategy(async (input: TInput) => {
      const result = await this.execute(input);
      const isValid = await predicate(result);
      return isValid ? result : undefined;
    });
  }

  /**
   * Create a new strategy that applies a side effect
   */
  tap(effect: (output: TOutput) => void | Promise<void>): AsyncLambdaStrategy<TInput, TOutput> {
    return new AsyncLambdaStrategy(async (input: TInput) => {
      const result = await this.execute(input);
      await effect(result);
      return result;
    });
  }

  /**
   * Create a new strategy with error handling
   */
  onError(handler: (error: Error) => TOutput | Promise<TOutput>): AsyncLambdaStrategy<TInput, TOutput> {
    return new AsyncLambdaStrategy(async (input: TInput) => {
      try {
        return await this.execute(input);
      } catch (error) {
        const err = error instanceof Error ? error : new Error(String(error));
        return handler(err);
      }
    });
  }

  /**
   * Create a new strategy with retry logic
   */
  retry(maxAttempts: number = 3, delayMs: number = 0): AsyncLambdaStrategy<TInput, TOutput> {
    return new AsyncLambdaStrategy(async (input: TInput) => {
      let lastError: unknown;
      for (let i = 0; i < maxAttempts; i++) {
        try {
          return await this.execute(input);
        } catch (error) {
          lastError = error;
          if (i < maxAttempts - 1 && delayMs > 0) {
            await new Promise((resolve) => setTimeout(resolve, delayMs));
          }
        }
      }
      throw lastError instanceof Error ? lastError : new Error(String(lastError));
    });
  }

  /**
   * Create a new strategy with timeout
   */
  timeout(timeoutMs: number): AsyncLambdaStrategy<TInput, TOutput> {
    return new AsyncLambdaStrategy((input: TInput) => {
      return new Promise<TOutput>((resolve, reject) => {
        const timer = setTimeout(() => {
          reject(new Error(`Strategy timeout after ${timeoutMs}ms`));
        }, timeoutMs);

        this.execute(input)
          .then((result) => {
            clearTimeout(timer);
            resolve(result);
          })
          .catch((err) => {
            clearTimeout(timer);
            reject(err);
          });
      });
    });
  }
}