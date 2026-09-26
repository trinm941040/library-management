import { Settings, X } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { Button } from '@/common/components/ui/button'
import { BrandLogo } from '@/common/components/BrandLogo'

import { navigationGroups, settingsNavigationItem } from '@/app/navigation'
import { canAll } from '@/shared/auth/permissions'

type SidebarProps = {
  isVisible: boolean
  permissions: string[]
  onClose: () => void
}

export function Sidebar({ isVisible, permissions, onClose }: SidebarProps) {
  const groups = navigationGroups
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => canAll(permissions, item.requiredPermissions)),
    }))
    .filter((group) => group.items.length > 0)
  return (
    <>
      <aside
        className={`sidebar ${isVisible ? 'is-open' : 'is-hidden'}`}
        aria-hidden={!isVisible}
        inert={!isVisible}
      >
        <Button
          variant="ghost"
          size="icon"
          type="button"
          className="sidebar-close"
          aria-label="Đóng thanh điều hướng"
          onClick={onClose}
        >
          <X aria-hidden="true" />
        </Button>
        <div className="brand">
          <BrandLogo variant="horizontal" tone="auto" className="brand-logo" />
        </div>

        <nav className="sidebar-nav" aria-label="Điều hướng chính">
          {groups.map((group) => (
            <div className="pb-2 pt-2" key={group.label}>
              <p className="nav-label pd-[10px]">{group.label}</p>
              {group.items.map(({ label, icon: Icon, path }) => (
                <NavLink
                  to={path}
                  key={label}
                  className={({ isActive }) => `nav-item justify-start ${isActive ? 'active' : ''}`}
                  onClick={() => {
                    if (window.matchMedia('(max-width: 1050px)').matches) onClose()
                  }}
                >
                  <Icon aria-hidden="true" />
                  <span>{label}</span>
                </NavLink>
              ))}
            </div>
          ))}
        </nav>

        {canAll(permissions, settingsNavigationItem.requiredPermissions) ? (
          <div className="sidebar-bottom">
            <NavLink
              to={settingsNavigationItem.path}
              className={({ isActive }) => `nav-item sidebar-settings ${isActive ? 'active' : ''}`}
              onClick={() => {
                if (window.matchMedia('(max-width: 1050px)').matches) onClose()
              }}
            >
              <Settings aria-hidden="true" />
              <span>{settingsNavigationItem.label}</span>
            </NavLink>
          </div>
        ) : null}
      </aside>

      {isVisible && (
        <Button
          variant="ghost"
          size="icon"
          className="sidebar-scrim"
          aria-label="Đóng menu"
          type="button"
          onClick={onClose}
        />
      )}
    </>
  )
}
