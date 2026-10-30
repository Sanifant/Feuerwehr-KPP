import { useEffect, useId, useRef } from 'react';
import type { ReactNode } from 'react';
import './VehicleModal.css';

export default function VehicleModal({ title, busy, onClose, children }: {
    title: string; busy: boolean; onClose: () => void; children: ReactNode;
}) {
    const ref = useRef<HTMLDialogElement>(null);
    const titleId = useId();
    useEffect(() => {
        const previous = document.activeElement;
        const dialog = ref.current;
        dialog?.showModal();
        return () => { dialog?.close(); if (previous instanceof HTMLElement) previous.focus(); };
    }, []);
    return <dialog ref={ref} className="vehicle-modal" aria-labelledby={titleId}
        onCancel={event => { event.preventDefault(); if (!busy) onClose(); }}>
        <header><h2 id={titleId}>{title}</h2><button type="button" className="btn-secondary" disabled={busy}
            aria-label="Dialog schließen" onClick={onClose}>×</button></header>
        {children}
    </dialog>;
}
