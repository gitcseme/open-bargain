export function FullPageSpinner() {
  return (
    <div className="flex min-h-svh items-center justify-center" role="status" aria-label="Loading">
      <div className="size-8 animate-spin rounded-full border-4 border-muted border-t-primary" />
    </div>
  )
}
