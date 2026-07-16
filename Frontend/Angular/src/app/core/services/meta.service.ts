import { Injectable, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';

export interface MetaConfig {
  titleKey?: string;
  descriptionKey?: string;
  keywords?: string;
  robots?: string;
  ogImage?: string;
}

@Injectable({ providedIn: 'root' })
export class MetaService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);
  private readonly translate = inject(TranslateService);
  private readonly appName = 'USM Inventory';

  setMeta(config: MetaConfig): void {
    const translated = config.titleKey
      ? this.translate.instant(config.titleKey)
      : this.appName;
    const fullTitle = translated !== this.appName
      ? `${translated} | ${this.appName}`
      : this.appName;

    this.title.setTitle(fullTitle);

    if (config.descriptionKey) {
      this.meta.updateTag({
        name: 'description',
        content: this.translate.instant(config.descriptionKey),
      });
    }
    if (config.keywords) {
      this.meta.updateTag({ name: 'keywords', content: config.keywords });
    }
    this.meta.updateTag({ name: 'robots', content: config.robots ?? 'noindex,nofollow' });
    if (config.ogImage) {
      this.meta.updateTag({ property: 'og:image', content: config.ogImage });
    }
  }
}
