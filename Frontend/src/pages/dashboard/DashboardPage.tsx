import { useState } from 'react'
import {
  ArrowUpRight,
  BookOpen,
  Check,
  ChevronDown,
  CircleAlert,
  Clock,
  Plus,
  RefreshCw,
  Search,
  Users,
  X,
} from 'lucide-react'
import { useAuth } from '../../auth/AuthProvider'
import { Button } from '@/common/components/ui/button'
import { Input } from '@/common/components/ui/input'
import { MetricCard } from './components/MetricCard'

const queue = [
  {
    member: 'Olivia Martin',
    item: 'The Midnight Library',
    date: 'Due today',
    tone: 'amber',
    initials: 'OM',
  },
  {
    member: 'Noah Williams',
    item: 'Tomorrow, and Tomorrow, and Tomorrow',
    date: '2 days overdue',
    tone: 'red',
    initials: 'NW',
  },
  {
    member: 'Amelia Brown',
    item: 'The Creative Act',
    date: '3 days overdue',
    tone: 'red',
    initials: 'AB',
  },
  {
    member: 'Ethan Davis',
    item: 'A Brief History of Time',
    date: 'Due tomorrow',
    tone: 'slate',
    initials: 'ED',
  },
]

export function DashboardPage() {
  const { user } = useAuth()
  const [search, setSearch] = useState('')
  const [notice, setNotice] = useState('')
  const filteredQueue = queue.filter((record) =>
    `${record.member} ${record.item}`.toLowerCase().includes(search.toLowerCase()),
  )
  const displayName = user?.displayName ?? 'User'

  const runCheckout = () => {
    setNotice('Checkout completed for Olivia Martin.')
    window.setTimeout(() => setNotice(''), 4000)
  }

  return (
    <div className="content-wrap">
      <div className="welcome-row">
        <div>
          <p className="eyebrow">THURSDAY, AUGUST 21, 2026</p>
          <h1>
            Good morning, {displayName} <span>✦</span>
          </h1>
          <p className="subheading">Here is what is happening across your library today.</p>
        </div>
        <Button className="primary-button" type="button" onClick={runCheckout}>
          <Plus aria-hidden="true" /> New checkout
        </Button>
      </div>
      {notice && (
        <div className="toast" role="status">
          <Check aria-hidden="true" />
          {notice}
          <Button
            variant="ghost"
            size="icon"
            type="button"
            aria-label="Dismiss notification"
            onClick={() => setNotice('')}
          >
            <X />
          </Button>
        </div>
      )}
      <section className="metrics" aria-label="Library overview metrics">
        <MetricCard
          label="Items checked out"
          value="1,284"
          delta="12.4%"
          positive
          icon={<BookOpen />}
          tone="blue"
        />
        <MetricCard
          label="Active members"
          value="2,847"
          delta="4.8%"
          positive
          icon={<Users />}
          tone="green"
        />
        <MetricCard
          label="Overdue items"
          value="36"
          delta="8.2%"
          icon={<CircleAlert />}
          tone="orange"
        />
        <MetricCard
          label="Due back today"
          value="58"
          delta="2.1%"
          positive
          icon={<Clock />}
          tone="violet"
        />
      </section>
      <div className="section-heading">
        <div>
          <h2>Action center</h2>
          <p>Prioritized tasks that need your attention.</p>
        </div>
        <Button variant="link" className="text-button" type="button">
          View all activity <ArrowUpRight aria-hidden="true" />
        </Button>
      </div>
      <section className="action-grid">
        <article className="focus-card">
          <div className="card-top">
            <span className="card-icon purple">
              <RefreshCw />
            </span>
            <span className="tag">CIRCULATION</span>
          </div>
          <h3>Keep the shelves moving.</h3>
          <p>There are 8 reservations ready for pickup and 12 returns waiting to be processed.</p>
          <Button variant="link" className="card-link" type="button">
            Open circulation desk <ArrowUpRight />
          </Button>
          <div className="card-arc" />
        </article>
        <article className="queue-card">
          <div className="queue-header">
            <div>
              <h3>Due soon & overdue</h3>
              <p>Follow up before items become a bigger problem.</p>
            </div>
            <span className="queue-total">36 total</span>
          </div>
          <div className="queue-search">
            <Search aria-hidden="true" />
            <Input
              aria-label="Search due and overdue records"
              placeholder="Search member or title..."
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>
          <div className="queue-list">
            {filteredQueue.map((record) => (
              <div className="queue-row" key={record.member}>
                <span className="avatar avatar-blue">{record.initials}</span>
                <span className="record-info">
                  <b>{record.member}</b>
                  <small>{record.item}</small>
                </span>
                <span className={`due-label ${record.tone}`}>{record.date}</span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="row-more"
                  aria-label={`Open ${record.member} record`}
                  type="button"
                >
                  •••
                </Button>
              </div>
            ))}
            {filteredQueue.length === 0 && (
              <p className="empty-state">No matching circulation records.</p>
            )}
          </div>
        </article>
      </section>
      <div className="section-heading lower">
        <div>
          <h2>Recent activity</h2>
          <p>Latest updates from your team.</p>
        </div>
        <Button variant="outline" className="filter-button" type="button">
          Last 7 days <ChevronDown />
        </Button>
      </div>
      <section className="activity-table">
        <div className="table-row table-head">
          <span>ACTIVITY</span>
          <span>STAFF MEMBER</span>
          <span>TIME</span>
          <span>STATUS</span>
        </div>
        <div className="table-row">
          <span className="activity-name">
            <span className="mini-icon green">
              <Check />
            </span>
            <b>Return processed</b>
            <small>The House in the Cerulean Sea</small>
          </span>
          <span>Alex Morgan</span>
          <span>10 min ago</span>
          <span className="status complete">Completed</span>
        </div>
        <div className="table-row">
          <span className="activity-name">
            <span className="mini-icon blue">
              <Users />
            </span>
            <b>New member added</b>
            <small>Membership #MB-2849</small>
          </span>
          <span>Jamie Davis</span>
          <span>42 min ago</span>
          <span className="status complete">Completed</span>
        </div>
      </section>
    </div>
  )
}
