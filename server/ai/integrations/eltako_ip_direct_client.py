"""
eltako_ip_direct_client.py

REFERENZ / AUSNAHMEFALL — nicht der empfohlene Weg für Jaimes.

Die neueren Eltako-Geräte der SERIES 62-IP (z. B. EUD62NPN-IP, FSR100-IPM,
FUD100-IPM, ESB64NP-IPM) bringen eine eigene lokale REST-API mit (WLAN,
kein FAM14/EnOcean-Gateway nötig). Solange es dafür noch keine fertige
Home-Assistant-Integration gibt, kann dieser direkte Client zum Testen
oder als Übergangslösung dienen.

WICHTIG: Die genauen Endpunkt-Pfade unten folgen dem offiziell
dokumentierten Muster (Login -> api-key, versionierte API, Geräte über
deviceGuid, Zustände über "characteristics"/identifier), sind aber vor
Produktiveinsatz gegen den offiziellen Entwicklerleitfaden zu prüfen:
https://www.eltako.com/fileadmin/downloads/de/_bedienung/Baureihe_62-IP_Entwicklerleitfaden_v2.pdf
Postman-Collection: http://eltako.com/redirect/api-series-62-ip

Empfehlung: Sobald eine Eltako-IP-Integration für Home Assistant existiert,
diesen Client durch home_assistant_client.py ersetzen — dann bleibt die
Architektur konsistent (Eltako -> Home Assistant -> Jaimes).
"""

from __future__ import annotations
import requests


class EltakoIPDeviceClient:
    def __init__(self, host: str, username: str, pop: str,
                 port: int = 443, api_major_version: int = 1,
                 verify_tls: bool = False, timeout: float = 5.0):
        """
        host: IP-Adresse des Eltako-IP-Geräts/-Gateways im lokalen Netz
        username / pop: Zugangsdaten (pop = "proof of possession", siehe Gerätelabel/App)
        verify_tls: Geräte nutzen oft selbstsignierte Zertifikate -> ggf. False
        """
        self.base_url = f"https://{host}:{port}/api/v{api_major_version}"
        self.verify_tls = verify_tls
        self.timeout = timeout
        self._session = requests.Session()
        self._api_key: str | None = None
        self._login(username, pop)

    def _login(self, username: str, pop: str) -> None:
        resp = self._session.post(
            f"{self.base_url}/login",
            json={"username": username, "pop": pop},
            timeout=self.timeout,
            verify=self.verify_tls,
        )
        resp.raise_for_status()
        self._api_key = resp.json()["api-key"]
        self._session.headers.update({"api-key": self._api_key})

    def list_devices(self) -> list[dict]:
        resp = self._session.get(
            f"{self.base_url}/devices", timeout=self.timeout, verify=self.verify_tls
        )
        resp.raise_for_status()
        return resp.json()

    def set_characteristic(self, device_guid: str, identifier: str, value) -> None:
        """
        Setzt einen Gerätezustand, z. B. identifier="on" mit value=True,
        oder identifier="brightness" mit value=30.
        """
        resp = self._session.put(
            f"{self.base_url}/devices/{device_guid}/characteristics/{identifier}",
            json={"value": value},
            timeout=self.timeout,
            verify=self.verify_tls,
        )
        resp.raise_for_status()

    def get_characteristic(self, device_guid: str, identifier: str):
        resp = self._session.get(
            f"{self.base_url}/devices/{device_guid}/characteristics/{identifier}",
            timeout=self.timeout,
            verify=self.verify_tls,
        )
        resp.raise_for_status()
        return resp.json().get("value")


# ---------- Beispielnutzung ----------

if __name__ == "__main__":
    client = EltakoIPDeviceClient(
        host="192.168.1.60",
        username="admin",
        pop="DAS_GERAETE_PASSWORT",
    )

    devices = client.list_devices()
    print("Gefundene Geräte:", devices)

    # Beispiel: erstes Gerät einschalten
    if devices:
        guid = devices[0]["deviceGuid"]
        client.set_characteristic(guid, "on", True)
        print("Gerät eingeschaltet.")
