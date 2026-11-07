interface Props {
  page: number
  pageSize: number
  totalCount: number
  onChange: (page: number) => void
}

export default function Pagination({ page, pageSize, totalCount, onChange }: Props) {
  const pageCount = Math.ceil(totalCount / pageSize)
  if (pageCount <= 1) return null

  return (
    <div className="pagination">
      <button className="button secondary small" disabled={page <= 1} onClick={() => onChange(page - 1)}>
        Önceki
      </button>
      <span className="muted small">{page} / {pageCount}</span>
      <button className="button secondary small" disabled={page >= pageCount} onClick={() => onChange(page + 1)}>
        Sonraki
      </button>
    </div>
  )
}
