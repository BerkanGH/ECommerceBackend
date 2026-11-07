import { useState, type ReactNode } from 'react'
import { isValidImageUrl } from '../utils/format'

interface Props {
  src: string | null
  alt: string
  className?: string
  children?: ReactNode
}

const placeholder = '/placeholder.svg'

export default function ProductImage({ src, alt, className = '', children }: Props) {
  const [failedSrc, setFailedSrc] = useState<string | null>(null)
  const usable = src && isValidImageUrl(src) && failedSrc !== src

  return (
    <div className={`product-image ${className}`}>
      <img
        src={usable ? src : placeholder}
        alt={usable ? alt : ''}
        loading="lazy"
        referrerPolicy="no-referrer"
        onError={() => setFailedSrc(src)}
      />
      {children}
    </div>
  )
}
