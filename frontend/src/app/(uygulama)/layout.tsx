import { OturumSaglayici } from "@/components/Oturum";
import UstMenu from "@/components/UstMenu";

export default function UygulamaDuzeni({ children }: { children: React.ReactNode }) {
  return (
    <OturumSaglayici>
      <UstMenu />
      {children}
    </OturumSaglayici>
  );
}
