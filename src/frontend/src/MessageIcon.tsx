export function MessageIcon() {
    const unreadCount = 0;
    
    return (
        <button className="message-icon" type="button" aria-label={`${unreadCount} neue Nachrichten`}>
            <span className="message-icon__bell" aria-hidden="true" />
            {unreadCount > 0 && (
                <span className="message-icon__badge">{unreadCount}</span>
            )}
        </button>
    );
}
