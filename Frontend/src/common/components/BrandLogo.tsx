import type { ImgHTMLAttributes } from 'react'
import { cn } from '@/utils/cn'

type BrandLogoVariant = 'symbol' | 'app-icon' | 'horizontal' | 'login'
type BrandLogoTone = 'default' | 'inverse' | 'auto'

type BrandLogoProps = Omit<ImgHTMLAttributes<HTMLImageElement>, 'src'> & {
  variant?: BrandLogoVariant
  tone?: BrandLogoTone
}

const BRAND_NAME = 'Hệ thống Quản lý Thư viện'
const BRAND_ASSET_ROOT = '/assets/brand'

const logoSources: Record<BrandLogoVariant, string> = {
  symbol: `${BRAND_ASSET_ROOT}/logo-symbol.svg`,
  'app-icon': `${BRAND_ASSET_ROOT}/logo-symbol-tile.svg`,
  horizontal: `${BRAND_ASSET_ROOT}/logo-horizontal.svg`,
  login: `${BRAND_ASSET_ROOT}/logo-login.svg`,
}

const logoDimensions: Record<BrandLogoVariant, { width: number; height: number }> = {
  symbol: { width: 512, height: 512 },
  'app-icon': { width: 512, height: 512 },
  horizontal: { width: 760, height: 176 },
  login: { width: 720, height: 440 },
}

const inverseLogoSources: Partial<Record<BrandLogoVariant, string>> = {
  horizontal: `${BRAND_ASSET_ROOT}/logo-horizontal-inverse.svg`,
  login: `${BRAND_ASSET_ROOT}/logo-login-inverse.svg`,
}

export function BrandLogo({
  variant = 'horizontal',
  tone = 'default',
  alt = BRAND_NAME,
  className,
  width,
  height,
  ...props
}: BrandLogoProps) {
  const dimensions = logoDimensions[variant]

  const inverseSource = inverseLogoSources[variant]

  if (tone === 'auto' && inverseSource) {
    return (
      <span
        className={cn('inline-block', className)}
        role={alt ? 'img' : undefined}
        aria-label={alt || undefined}
      >
        <img
          {...props}
          src={logoSources[variant]}
          alt=""
          aria-hidden="true"
          width={width ?? dimensions.width}
          height={height ?? dimensions.height}
          className="block h-auto w-full dark:hidden"
        />
        <img
          {...props}
          src={inverseSource}
          alt=""
          aria-hidden="true"
          width={width ?? dimensions.width}
          height={height ?? dimensions.height}
          className="hidden h-auto w-full dark:block"
        />
      </span>
    )
  }

  const src = tone === 'inverse' && inverseSource ? inverseSource : logoSources[variant]

  return (
    <img
      {...props}
      src={src}
      alt={alt}
      width={width ?? dimensions.width}
      height={height ?? dimensions.height}
      className={className}
    />
  )
}
