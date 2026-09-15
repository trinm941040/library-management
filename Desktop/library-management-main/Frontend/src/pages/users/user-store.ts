export type UserRole = 'Administrator' | 'Librarian' | 'Member'
export type UserStatus = 'active' | 'locked'

export type SystemUser = {
  id: string
  name: string
  email: string
  role: UserRole
  status: UserStatus
  createdAt: string
}

const STORAGE_KEY = 'library-system-users'

const initialUsers: SystemUser[] = [
  {
    id: '1',
    name: 'Jamie Davis',
    email: 'jamie.davis@library.local',
    role: 'Administrator',
    status: 'active',
    createdAt: '2026-08-01',
  },
  {
    id: '2',
    name: 'Alex Morgan',
    email: 'alex.morgan@library.local',
    role: 'Librarian',
    status: 'active',
    createdAt: '2026-08-04',
  },
  {
    id: '3',
    name: 'Olivia Martin',
    email: 'olivia.martin@example.com',
    role: 'Member',
    status: 'active',
    createdAt: '2026-08-08',
  },
  {
    id: '4',
    name: 'Noah Williams',
    email: 'noah.williams@example.com',
    role: 'Member',
    status: 'locked',
    createdAt: '2026-08-12',
  },
]

export function loadUsers(): SystemUser[] {
  const savedUsers = localStorage.getItem(STORAGE_KEY)

  if (!savedUsers) return initialUsers

  try {
    return JSON.parse(savedUsers) as SystemUser[]
  } catch {
    return initialUsers
  }
}

export function saveUsers(users: SystemUser[]) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(users))
}
