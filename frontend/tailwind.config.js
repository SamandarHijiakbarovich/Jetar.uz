/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        // Dizayn palitrasi — Jetar Arxitektura, 05-bo'lim
        ink: '#0B0B14',           // sahifa foni
        'ink-2': '#08080F',       // eng chuqur qatlam
        surface: '#16162A',       // kartochka
        'surface-2': '#101024',   // ikkilamchi bo'lim foni
        'surface-3': '#1D1D38',
        elevated: '#1C1C36',      // ko'tarilgan kartochka (hover, modal)
        field: '#0F0F1E',         // input foni
        brand: {
          DEFAULT: '#FF6B35',
          300: '#FFB08A',
          400: '#FF9558',
          500: '#FF8A50',
          600: '#FF6B35',
        },
        muted: '#9A9ABE',         // ikkilamchi matn
        dim: '#6E6E92',           // uchinchi darajali matn
        soft: '#C5C5E0',
        'soft-2': '#B8B8D4',
        'soft-3': '#D5D5EC',
        success: '#10B981',
        'success-fg': '#6EE7B7',
        warning: '#F59E0B',
        'warning-fg': '#FCD34D',
        danger: '#EF4444',
        'danger-fg': '#FCA5A5',
        info: '#3B82F6',
        'info-fg': '#93C5FD',
      },
      fontFamily: {
        display: ['Poppins', 'system-ui', 'sans-serif'],
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['"JetBrains Mono"', 'ui-monospace', 'monospace'],
      },
      borderRadius: {
        card: '18px',
        panel: '20px',
        hero: '22px',
      },
      boxShadow: {
        brand: '0 12px 30px rgba(255,107,53,.35)',
        'brand-sm': '0 6px 16px rgba(255,107,53,.3)',
        'brand-glow': '0 0 0 1px rgba(255,107,53,.35), 0 18px 48px -12px rgba(255,107,53,.45)',
        card: '0 20px 40px rgba(0,0,0,.45)',
        'card-lg': '0 22px 44px rgba(0,0,0,.5)',
        panel: '0 30px 70px rgba(0,0,0,.5)',
        // Yuqoridan nozik yorug'lik — kartochkalarga hajm beradi.
        rim: 'inset 0 1px 0 0 rgba(255,255,255,.06)',
        'rim-lg': 'inset 0 1px 0 0 rgba(255,255,255,.09), 0 24px 48px -16px rgba(0,0,0,.7)',
      },
      backgroundImage: {
        'grid-fade':
          'linear-gradient(rgba(255,255,255,.025) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,.025) 1px, transparent 1px)',
      },
      backgroundSize: {
        grid: '32px 32px',
      },
      keyframes: {
        jfloat: {
          '0%,100%': { transform: 'translateY(0) rotate(0deg)' },
          '50%': { transform: 'translateY(-18px) rotate(4deg)' },
        },
        jfade: {
          from: { opacity: '0', transform: 'translateY(16px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        jpulse: {
          '0%,100%': { opacity: '.45' },
          '50%': { opacity: '.9' },
        },
        jrise: {
          from: { opacity: '0', transform: 'translateY(10px)' },
          to: { opacity: '1', transform: 'translateY(0)' },
        },
        jshimmer: {
          '100%': { transform: 'translateX(100%)' },
        },
        jorbit: {
          '0%,100%': { transform: 'translate(0,0) scale(1)' },
          '50%': { transform: 'translate(6%,-6%) scale(1.08)' },
        },
      },
      animation: {
        jfloat: 'jfloat 7s ease-in-out infinite',
        jfade: 'jfade .4s ease-out both',
        jpulse: 'jpulse 2s infinite',
        jrise: 'jrise .45s cubic-bezier(.2,.7,.3,1) both',
        jshimmer: 'jshimmer 1.6s infinite',
        jorbit: 'jorbit 14s ease-in-out infinite',
      },
    },
  },
  plugins: [],
}
