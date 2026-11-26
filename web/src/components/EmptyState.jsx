import styles from './EmptyState.module.css';

export default function EmptyState({ icon: Icon, title, description, children }) {
  return (
    <section className={styles.empty}>
      {Icon && (
        <div className={styles.icon} aria-hidden="true">
          <Icon size={28} />
        </div>
      )}
      <h2 className={styles.title}>{title}</h2>
      {description && <p className={styles.description}>{description}</p>}
      {children}
    </section>
  );
}
